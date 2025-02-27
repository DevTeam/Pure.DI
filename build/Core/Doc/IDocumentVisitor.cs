// ReSharper disable UnusedParameter.Global

namespace Build.Core.Doc;

interface IDocumentVisitor<T>
{
    Task<T> StartTypeVisitAsync(T ctx, DocumentType type, CancellationToken cancellationToken);

    Task<T> FinishTypeVisitAsync(T ctx, DocumentType type, CancellationToken cancellationToken);

    Task<T> StartMemberVisitAsync(T ctx, DocumentMember member, CancellationToken cancellationToken);

    Task<T> FinishMemberVisitAsync(T ctx, DocumentMember member, CancellationToken cancellationToken);

    Task<T> StartElementVisitAsync(T ctx, DocumentElement element, CancellationToken cancellationToken);

    Task<T> FinishElementVisitAsync(T ctx, DocumentElement element, CancellationToken cancellationToken);

    Task<T> TextVisitAsync(T ctx, DocumentText text, CancellationToken cancellationToken);

    Task<T> CompletedAsync(T ctx, CancellationToken cancellationToken);
}