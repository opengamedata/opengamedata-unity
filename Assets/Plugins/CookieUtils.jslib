mergeInto(LibraryManager.library, {

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

    SetCookie: function (name, val, days) {
        var name = Pointer_stringify(name);
        var val = Pointer_stringify(val);
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        document.cookie = name + "=" + val + "; expires=" + date.toGMTString() + "; path=/";
    },

});
