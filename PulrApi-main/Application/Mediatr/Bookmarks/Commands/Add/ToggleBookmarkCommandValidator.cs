using Core.Domain.Enums;
using FluentValidation;

namespace Core.Application.Mediatr.Bookmarks.Commands.Add;

public class ToggleBookmarkCommandValidator : AbstractValidator<ToggleBookmarkCommand>
{
    public ToggleBookmarkCommandValidator()
    {
        RuleFor(e => e.PostUid).NotEmpty().WithMessage("Post Uid is required");
    }
}