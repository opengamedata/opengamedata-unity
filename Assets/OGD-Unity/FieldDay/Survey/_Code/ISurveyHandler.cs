using System.Collections.Generic;

namespace FieldDay
{
    public interface ISurveyHandler
    {
        void HandleSurveyResponse(Dictionary<string, string> surveyResponses);
    }
}
