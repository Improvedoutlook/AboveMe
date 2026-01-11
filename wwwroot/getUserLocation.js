// JS interop for geolocation
window.getUserLocation = function () {
    return new Promise(function (resolve, reject) {
        if (!navigator.geolocation) {
            resolve(null);
            return;
        }
        navigator.geolocation.getCurrentPosition(function (position) {
            var result = {
                Latitude: position.coords.latitude,
                Longitude: position.coords.longitude
            };
            resolve(result);
        }, function (error) {
            resolve(null);
        }, {
            enableHighAccuracy: true,
            timeout: 10000,
            maximumAge: 300000
        });
    });
};
