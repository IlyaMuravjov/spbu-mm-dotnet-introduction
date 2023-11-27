#!/bin/bash
curl -s http://localhost/weather/openweathermap.org | jq > "../responses/openweathermap_org.json"
