using System.Collections.Generic;

namespace FillingHintService.Model
{
    public class Hint
    {
        public List<HintCondition> HintCondition { get; set; }
        public List<HintText> HintText { get; set; }
    }
}