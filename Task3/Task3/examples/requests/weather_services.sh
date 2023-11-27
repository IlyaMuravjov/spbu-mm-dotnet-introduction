#!/bin/bash
curl -s http://localhost/weather/services | jq > "../responses/weather_services.json"
