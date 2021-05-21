mergeInto(LibraryManager.library, {

    FetchSurvey: function () {
        remoteConfig.fetchAndActivate()
            .then(function() {
                var surveyString = remoteConfig.getString("survey_string");
                console.log(surveyString);
                console.log(typeof(surveyString));
                var bufferSize = lengthBytesUTF8(surveyString) + 1;
                var buffer = _malloc(bufferSize);
                stringToUTF8(surveyString, buffer, bufferSize);
                console.log(buffer);
                console.log(typeof(buffer));
                return buffer;
            })
            .catch(function(err) {
                return(err);
            });
    },

    StringReturnValueFunction: function () {
        var returnStr = "bla";
        var bufferSize = lengthBytesUTF8(returnStr) + 1;
        var buffer = _malloc(bufferSize);
        stringToUTF8(returnStr, buffer, bufferSize);
        return buffer;
    }

});
