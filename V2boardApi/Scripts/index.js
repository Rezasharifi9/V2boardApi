console.log("hh");
if ("serviceWorker" in navigator) {
    
    navigator.serviceWorker.register(window.location.origin + "/sw.js").then(registeration => {

       
        
        console.log("Sw Registerd");
        console.log(registeration);

    }).catch(error => {
        console.log("Sw Failed");
        console.log(error);

    });
}


function setCookie(name, value, days) {
    var expires = "";
    if (days) {
        var date = new Date();
        date.setTime(date.getTime() + (days * 24 * 60 * 60 * 1000));
        expires = "; expires=" + date.toUTCString();
    }
    document.cookie = name + "=" + (value || "") + expires + "; path=/";
}
function getCookie(name) {
    var nameEQ = name + "=";
    var ca = document.cookie.split(';');
    for (var i = 0; i < ca.length; i++) {
        var c = ca[i];
        while (c.charAt(0) == ' ') c = c.substring(1, c.length);
        if (c.indexOf(nameEQ) == 0) return c.substring(nameEQ.length, c.length);
    }
    return null;
}