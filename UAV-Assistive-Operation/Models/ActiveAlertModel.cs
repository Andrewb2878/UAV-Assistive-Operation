namespace UAV_Assistive_Operation.Models
{
    public class ActiveAlertModel
    {
        public int Priority { get; set; }
        public string Message { get; set; }

        public bool IsCritical => Priority <= 3;

    }
}
