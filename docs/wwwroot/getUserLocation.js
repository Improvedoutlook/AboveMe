// JS interop for geolocation
window.getUserLocation = function () {
    return new Promise(function (resolve, reject) {
        if (!navigator.geolocation) {
            resolve(null);
            return;
        }
        navigator.geolocation.getCurrentPosition(function (position) {
            resolve({
                latitude: position.coords.latitude,
                longitude: position.coords.longitude
            });
        }, function (error) {
            resolve(null);
        });
    });
};
