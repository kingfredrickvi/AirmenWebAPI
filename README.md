
# Airmen Web API

This plugin for AirmenModEngine adds an HTTP web interface for the Airmen servers.

Requires: https://github.com/kingfredrickvi/AirmenModEngine

## How to install

Example script

```
# Assumes AirmenModEngine is installed

$AIRMEN_PATH=/path/to/steam/airmen

mkdir -p $AIRMEN_PATH/plugins

wget https://github.com/kingfredrickvi/AirmenWebAPI/releases/download/1.0/webapi-v1.0.tar.gz

tar -xzvf webapi-v1.0.tar.gz -C $AIRMEN_PATH/plugins
```

## API Info

Host: 0.0.0.0
Port: 7788

Nginx Example Reverse

```
    location /api {
      proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
      proxy_set_header Host $host;

      proxy_pass http://localhost:7788;
      add_header Access-Control-Allow-Origin *;
      proxy_set_header Access-Control-Allow-Origin *;

      proxy_http_version 1.1;
    }
```
