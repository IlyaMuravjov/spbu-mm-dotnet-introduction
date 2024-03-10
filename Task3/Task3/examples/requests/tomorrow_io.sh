#!/bin/bash
curl -s http://localhost/weather/tomorrow.io | jq > "../responses/tomorrow_io.json"
