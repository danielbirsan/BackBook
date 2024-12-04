using System.ComponentModel.DataAnnotations;

namespace proiect_daw.Validations
{
    public class Max200CharsValidation : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is string stringValue && stringValue.Length < 200)
            {
                return ValidationResult.Success;
            }
            return new ValidationResult("Continutul trebuie sa fie sub 200 de caractere");
        }
    }
}
