using System;

public static class DateTimeExtensions
{
    public static int GetAgeAt(this DateTime dateOfBirth, DateTime referenceDate)
    {
        var age = referenceDate.Year - dateOfBirth.Year;

        if (referenceDate.Date < dateOfBirth.Date.AddYears(age))
        {
            age--;
        }

        return age;
    }
}