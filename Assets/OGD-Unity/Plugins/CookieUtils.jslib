/**
 * @fileoverview Contains helper functions for getting and setting cookie values in a
 * WebGL build. See SimpleLog and SimpleLogUtils for import and use of the functions.
 */
mergeInto(LibraryManager.library, {

    /**
     * Find a given cookie within the web page's full cookie and return its value.
     * @param {string} name The name of the cookie to find.
     * @return {string} The retrieved value for the specified cookie, or an empty string
     *      if the cookie couldn't be found.
     */
    GetCookie: function (name) {
        var full_cookie = decodeURIComponent(document.cookie);
        var cookies = full_cookie.split(';');
        var name = Pointer_stringify(name) + "=";

        for (var i = 0; i < cookies.length; ++i) {
            var cookie = cookies[i];

            while (cookie.charAt(0) == ' ') {
                cookie = cookie.substring(1);
            }

            if (cookie.indexOf(name) == 0) {
                return cookie.substring(name.length, cookie.length);
            }
        }

        return "";
    },

    /**
     * Set a cookie with a given name, value, and unique ID using the current datetime.
     * @param {string} name The name for the cookie to set.
     * @param {string} val The value for the given cookie.
     * @param {number} days The number of days to add to the current datetime for the cookie's ID.
     */
    SetCookie: function (name, val, days) {
        var name = Pointer_stringify(name);
        var val = Pointer_stringify(val);
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = name + "=" + val + "; expires=" + date.toGMTString() + "; path=/";
    },

});
