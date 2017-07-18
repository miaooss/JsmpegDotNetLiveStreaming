/*
**  Created by Jeremy Lb on 18/07/2017
**
*/

function PlayerSource (url, options) {
  this.url = url;
  this.options = options;
  this.destination = null;
  this.ws = null;
  this.streaming = true;
  this.progress = 0;

  this.reconnectInterval = options.reconnectInterval !== undefined ? options.reconnectInterval : 5;
  this.shouldAttemptReconnect = !!this.reconnectInterval;
  this.established = false;
  this.reconnectTimeoutId = 0;
  this.onKeepAliveMessageReceived = null;
}

PlayerSource.prototype.connect = function(destination) {
	this.destination = destination;
};

PlayerSource.prototype.destroy = function() {
	clearTimeout(this.reconnectTimeoutId);
	this.shouldAttemptReconnect = false;
	this.ws.close();
};

PlayerSource.prototype.start = function() {
	this.shouldAttemptReconnect = this.shouldAttemptReconnect && !!this.reconnectInterval;
	this.established = false;
  this.progress = 0;

	this.ws =  this.options.protocols ? new WebSocket(this.url, this.options.protocols) : new WebSocket(this.url);
	this.ws.binaryType = 'arraybuffer';
	this.ws.onmessage = this.onMessage.bind(this);
	this.ws.onopen = this.onOpen.bind(this);
	this.ws.onerror = this.onClose.bind(this);
	this.ws.onclose = this.onClose.bind(this);
};

PlayerSource.prototype.resume = function(secondsHeadroom) {
	// Nothing to do here
};

PlayerSource.prototype.onOpen = function() {
  this.log("Connect to " + this.url);
  this.progress = 1;
	this.established = true;
};

PlayerSource.prototype.onClose = function() {
  this.log("Disconnected from " + this.url);
	if (this.shouldAttemptReconnect) {
		clearTimeout(this.reconnectTimeoutId);
		this.reconnectTimeoutId = setTimeout(function(){
			this.start();
		}.bind(this), this.reconnectInterval*1000);
	}
};

PlayerSource.prototype.onMessage = function(ev) {
    if(typeof ev.data == "string") {
        this.cmd(JSON.parse(ev.data));
    }
  	else if (this.destination) {
  		this.destination.write(ev.data);
  	}
};

PlayerSource.prototype.cmd = function(cmdobj){
  if(cmdobj.action || cmdobj.Action)
    switch (cmdobj.action || cmdobj.Action) {
      case "Init":
        this.log("init message recieved");
        break;
      case "RTSPLostConnection":
        this.log("RTSPLostConnection recieved");
        break;
      case "EndPlayBackVideo":
        this.log("EndPlayBackVideo recieved");
        this.shouldAttemptReconnect = false;
        break;
      case "KeepAlive":
        this.log("KeepAlive recieved");
        //Should Send Message to keep the connection open
        if(this.onKeepAliveMessageReceived && this.onKeepAliveMessageReceived())
          this.sendKeepConnectionOpen();
        break;
      case "KeepAliveTimeOut":
        this.log("KeepAliveTimeOut recieved");
        this.shouldAttemptReconnect = false;
        break;
      default:
    }
};

PlayerSource.prototype.log = function(msg){
  if(console.log)
    console.log(msg);
};

PlayerSource.prototype.sendKeepConnectionOpen = function() {
  this.shouldAttemptReconnect = true;
  var message = JSON.parse({action: "keepConnectionOpen"});
  this.ws.send(message);
  log("Sent " + message);
};
