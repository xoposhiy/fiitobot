using Newtonsoft.Json;

namespace fiitobot.Services
{
    public class BrsStudentMark
    {
        /*
        // "loadAction":"tlekc",
        // "practice":"50.00",
        // "studentUuid":"studen195e3pg0000n8h1cs9rv48q2gkundigr195e3pg0000n1i5fdmhhscasqg",
        // "subgroupsTitles":"ИЕНИМ, ФИИТ набор 2020 (осень 2022), л1, Егоров П.В.<br>ИЕНИМ, ФИИТ набор 2020 (осень 2022), п1, Егоров П.В.<br>ИЕНИМ, ФИИТ набор 2020 (осень 2022), экзамен1, Егоров П.В.",
        // "techGroupId":72681,
        // "betweenCourseProject":"0.00",
        // "studentPersonalNumber":"56000310",
        // "studentGroup":"МЕН-300801",
        // "techGroupDebarMessage":"",
        // "total":"100.00",
        // "studentStatus":"Активный",
        // "lecture":"50.00",
        // "id":7426735,
        // "techGroupTitle":"Спортивное программирование\\л1",
        // "debarMessage":"КМ, у которых балл отсутствует или равен 0: <br>\"Домашняя работа\",<br>\"Домашняя работа\",<br>\"экзамен\".",
        // "teacherName":"",
        // "moduleTitle":"Спортивное программирование",
        // "newDisciplineLoad":"8aca6168831da09a01833357c1ee0e50",
        // "existsMoreOneCA":false,
        // "studentFio":"Базеев Дамир Шевкетович",
        // "additionalPractice":"0.0",
        // "subgroupsITS":"",
        // "courseWork":"0.00",
        // "laboratory":"0.00",
        // "studentName":"Базеев Д.Ш.",
        // "courseProject":"0.00",
        // "online":"0.00",
        // "olympiad":null
        */

        // "studentUuid":"studen195e3pg0000n8h1cs9rv48q2gkundigr195e3pg0000n1i5fdmhhscasqg",
        // "studentPersonalNumber":"56000310",
        // "studentGroup":"МЕН-300801",
        // "total":"100.00",
        // "studentStatus":"Активный",
        // "moduleTitle":"Спортивное программирование",
        // "studentFio":"Базеев Дамир Шевкетович",
        // "mark":"Отлично",

        [JsonProperty("studentUuid")]
        public string StudentUuid { get; set; }

        [JsonProperty("studentPersonalNumber")]
        public string StudentPersonalNumber { get; set; }

        [JsonProperty("studentGroup")]
        public string StudentGroup { get; set; }

        [JsonProperty("total")]
        public string Total { get; set; }

        [JsonProperty("studentStatus")]
        public string StudentStatus { get; set; }

        [JsonProperty("moduleTitle")]
        public string ModuleTitle { get; set; }

        [JsonProperty("studentFio")]
        public string StudentFio { get; set; }

        [JsonProperty("mark")]
        public string Mark { get; set; }
        public string ContainerName { get; set; }

        public bool IsRealMark => Mark != "Не должен сдавать"
                                  && Mark != "Не выбрана"
                                  && !string.IsNullOrEmpty(Mark)
                                  && Mark != "0.00"
                                  && !string.IsNullOrEmpty(Total);

        public override string ToString()
        {
            return $"{StudentGroup} {StudentFio} {ModuleTitle} {Total}";
        }
    }
}
