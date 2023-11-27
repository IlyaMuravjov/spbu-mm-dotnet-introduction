#!/bin/bash
curl -s http://localhost/weather | jq > "../responses/weather.json"
