using FluentValidation;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public class UploadDocumentCommandValidator : AbstractValidator<UploadDocumentCommand>
{
    private const long MaxSizeBytes = 50 * 1024 * 1024; // 50 MB

    // Whitelist of document/image types acceptable for rental contracts. Blocks active content
    // (HTML/SVG/JS) that could be served back and executed in the browser.
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain",
    };

    public UploadDocumentCommandValidator()
    {
        RuleFor(x => x.FileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(127)
            .Must(m => AllowedMimeTypes.Contains(m))
            .WithMessage("Tipo de archivo no permitido. Aceptados: PDF, imágenes, Word, Excel y texto.");
        RuleFor(x => x.SizeBytes).GreaterThan(0).LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("El archivo no puede superar los 50 MB.");
        RuleFor(x => x.ContractId).NotEmpty();
        RuleFor(x => x.Content).NotNull();
    }
}
