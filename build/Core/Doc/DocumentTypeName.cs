namespace Build.Core.Doc;

public record DocumentTypeName(
    string NamespaceName,
    string TypeName,
    string MemberName = "");