
//-----------------------------------------------------------------------------------------//
//----------------------------------> STANDARD VARIABLE <----------------------------------//
//-----------------------------------------------------------------------------------------//
var postReturn = null;
var postReturnValue = null;
var pathArray = window.location.href.split('/');
var protocol = pathArray[0];
var host = pathArray[2];
var app = pathArray[3];
var url = '';
if (host.indexOf("localhost") > -1) {
    url = protocol + '//' + host + '/';
} else {
    url = protocol + '//' + host + '/' + app + '/';
}