# Arduino Joystick Remote Control
.NET Core Worker that fetches storm data from STARNET/USP, calculates distance from home and send information to local network using MQTT

## Published Topics on MQTT
- Storm/1KM
	Storms detected within 1KM of House in position the last 15 MIN
	
- Storm/2KM
	Storms detected within 1KM - 2KM of the position defined in the last 15 MIN
- Storm/5KM
	Storms detected within 2KM - 4KM of the position defined in the last 15 MIN
	
- Storm/8KM
	Storms detected within 4KM - 8KM of the position defined in the last 15 MIN
- Storm/NEAREST
	Nearest storm detect (in Km) of the position defined in the last 15 MIN
	
- Storm/LASTDATE
	Last DateTime a Storm was Detected.

## AppSettings Config
- "MQTTServer": "10.0.1.154"
IP of the MQTT Server: Does not Support Authentication

- "Intervalo": 60000
Interval in milliseconds to fetch the storm URL
- "StormURL": "https://www.starnet.iag.usp.br/linet/linet_0-15.kml"
Storm URL from STARNET

- "LatitudeCasa": -28.48164
Your house Latitud Position

- "LongitudeCasa": -47.61819
Your house Longitud Position
