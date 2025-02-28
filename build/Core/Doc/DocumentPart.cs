namespace Build.Core.Doc;

public record DocumentPart(
    string NamespaceName,
    string TypeName,
    string MemberName = "");