// JS interop for geolocation
window.getUserLocation = function () {
    console.log("getUserLocation called");
    return new Promise(function (resolve, reject) {
        if (!navigator.geolocation) {
            console.log("Geolocation not supported by browser");
            resolve(null);
            return;
        }
        console.log("Requesting geolocation permission...");
        navigator.geolocation.getCurrentPosition(function (position) {
            console.log("Geolocation success:", position.coords.latitude, position.coords.longitude);
            var result = {
                Latitude: position.coords.latitude,
                Longitude: position.coords.longitude
            };
            console.log("Returning result:", JSON.stringify(result));
            resolve(result);
        }, function (error) {
            console.log("Geolocation error:", error.code, error.message);
            resolve(null);
        }, {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 300000
        });
    });
};
