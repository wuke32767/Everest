#!/usr/bin/env bash

# Assume default script location
EVERESTPATH=${EVERESTPATH:-"$(dirname "$0")/../.."}

# Setting this variable is mandatory for the scripts to function, you can either set it before running the script
# or hardcode it in here.
# CELESTEGAMEPATH=/path/to/the/game
if [[ -z "${CELESTEGAMEPATH}" ]]; then
  echo "\$CELESTEGAMEPATH is not set! Please assign it with your target celeste game directory."
  exit 1
fi

# For TAS consistency it usually is necessary to build in Release mode, but for anything else Debug will suffice
# and provide better debugger support
CONFIGURATION=${CONFIGURATION:-"Debug"}

ARTIFACT_DIR=${ARTIFACT_DIR:-"$EVERESTPATH/artifacts"}
