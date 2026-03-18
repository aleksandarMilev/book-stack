namespace BookStack.Infrastructure.Validation;

using System.ComponentModel.DataAnnotations;
using Services.ImageValidator;

public sealed class ImageUploadAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(
        object? value,
        ValidationContext validationContext)
    {
        if (value is null) 
        {
            return ValidationResult.Success;
        }

        if (value is not IFormFile file)
        {
             return new("Invalid file.");
        }

        var imageValidator = (IImageValidator?)validationContext
            .GetService(typeof(IImageValidator));

        if (imageValidator is null) 
        {
            return new("ImageValidator service is not registrated in the IoC container.");
        }

        var validationReuslt = imageValidator.ValidateImageFile(file);
        if (!validationReuslt.Succeeded)
        {
            return new(validationReuslt.ErrorMessage);
        }

        return ValidationResult.Success;
    }
}
