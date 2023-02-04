namespace fiitobot.Services.Commands
{
    public class Rubrics
    {
        public static string Semester(int year, int yearPart, int courseNumber)
        {
            var semNumber = (courseNumber - 1) * 2 + yearPart;
            return $"Семестр {semNumber} ({year})";
        }
    }
}
