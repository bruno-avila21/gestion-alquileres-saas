using FluentValidation;

namespace GestionAlquileres.Application.Features.Documents.Commands;

public class DeleteDocumentCommandValidator : AbstractValidator<DeleteDocumentCommand>
{
    public DeleteDocumentCommandValidator()
    {
        RuleFor(x => x.DocumentId).NotEmpty();
    }
}
