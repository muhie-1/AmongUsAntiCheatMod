using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security;

namespace AntiCheat.UpdateNotifier;

/// <summary>Reads this mod's declared version without loading the plugin or its dependencies.</summary>
public static class InstalledPluginVersion
{
    private const string PluginGuid = "local.amongus.anticheat";

    /// <summary>
    /// Inspects only <paramref name="pluginPath"/>. Returns null when the file is unavailable,
    /// malformed, or does not declare this mod's BepInEx plugin attribute.
    /// </summary>
    public static string? Read(string pluginPath)
    {
        if (string.IsNullOrWhiteSpace(pluginPath)) return null;

        try
        {
            using var stream = new FileStream(pluginPath, FileMode.Open, FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            using var image = new PEReader(stream);
            if (!image.HasMetadata) return null;

            MetadataReader metadata = image.GetMetadataReader();
            foreach (TypeDefinitionHandle typeHandle in metadata.TypeDefinitions)
            {
                foreach (CustomAttributeHandle attributeHandle in metadata.GetTypeDefinition(typeHandle).GetCustomAttributes())
                {
                    CustomAttribute attribute = metadata.GetCustomAttribute(attributeHandle);
                    if (!IsPluginConstructor(metadata, attribute.Constructor)) continue;

                    BlobReader value = metadata.GetBlobReader(attribute.Value);
                    if (value.ReadUInt16() != 1) continue;
                    string? guid = value.ReadSerializedString();
                    string? name = value.ReadSerializedString();
                    string? version = value.ReadSerializedString();

                    // BepInPlugin uses three fixed string arguments and no named arguments.
                    if (value.ReadUInt16() != 0 || value.RemainingBytes != 0) continue;
                    if (string.Equals(guid, PluginGuid, StringComparison.Ordinal) &&
                        !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(version))
                        return version;
                }
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or
            BadImageFormatException or ArgumentException or InvalidOperationException or
            NotSupportedException or SecurityException)
        {
            // A removed, inaccessible, or invalid file is not an installed version.
        }

        return null;
    }

    public static bool TryRead(string pluginPath, [NotNullWhen(true)] out string? version)
    {
        version = Read(pluginPath);
        return version is not null;
    }

    private static bool IsPluginConstructor(MetadataReader metadata, EntityHandle constructor)
    {
        EntityHandle declaringType;
        BlobHandle signatureHandle;
        StringHandle methodName;

        switch (constructor.Kind)
        {
            case HandleKind.MemberReference:
                MemberReference reference = metadata.GetMemberReference((MemberReferenceHandle)constructor);
                declaringType = reference.Parent;
                signatureHandle = reference.Signature;
                methodName = reference.Name;
                break;
            case HandleKind.MethodDefinition:
                MethodDefinition method = metadata.GetMethodDefinition((MethodDefinitionHandle)constructor);
                declaringType = method.GetDeclaringType();
                signatureHandle = method.Signature;
                methodName = method.Name;
                break;
            default:
                return false;
        }

        if (!metadata.StringComparer.Equals(methodName, ".ctor")) return false;

        StringHandle typeName;
        StringHandle typeNamespace;
        switch (declaringType.Kind)
        {
            case HandleKind.TypeReference:
                TypeReference reference = metadata.GetTypeReference((TypeReferenceHandle)declaringType);
                typeName = reference.Name;
                typeNamespace = reference.Namespace;
                break;
            case HandleKind.TypeDefinition:
                TypeDefinition definition = metadata.GetTypeDefinition((TypeDefinitionHandle)declaringType);
                typeName = definition.Name;
                typeNamespace = definition.Namespace;
                break;
            default:
                return false;
        }

        if (!metadata.StringComparer.Equals(typeNamespace, "BepInEx") ||
            !metadata.StringComparer.Equals(typeName, "BepInPlugin")) return false;

        // Exact CLI signature: instance void .ctor(string, string, string).
        BlobReader signature = metadata.GetBlobReader(signatureHandle);
        return signature.RemainingBytes == 6 && signature.ReadByte() == 0x20 &&
            signature.ReadByte() == 3 && signature.ReadByte() == 0x01 &&
            signature.ReadByte() == 0x0e && signature.ReadByte() == 0x0e && signature.ReadByte() == 0x0e;
    }
}
