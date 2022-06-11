namespace fiitobot
{
    public class Detail
    {
        public Detail(string rubric, string parameter, string value, int sourceId)
        {
            Rubric = rubric;
            Parameter = parameter;
            Value = value;
            SourceId = sourceId;
        }

        public string Rubric;
        public string Parameter;
        public string Value;
        public int SourceId;
    };
}
