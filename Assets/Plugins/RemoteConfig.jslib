mergeInto(LibraryManager.library, {

    FetchSurvey: function () {
        remoteConfig.fetchAndActivate()
            .then(function() {
                var surveyString = remoteConfig.getString("survey_string");
                unityInstance.SendMessage("Survey", "LoadSurvey", surveyString);
            })
            .catch(function(err) {
                console.log(err);
            });
    }

});
