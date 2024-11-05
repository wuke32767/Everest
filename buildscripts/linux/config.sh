#!/usr/bin/env bash

# Assume default script location
EVERESTPATH=${EVERESTPATH:-"$(dirname "$0")/../.."}

# Feel free to adjust it to where your is
CELESTEGAMEPATH=${CELESTEGAMEPATH:-"$EVERESTPATH/_celestegame"}

# For TAS consistency it usually is necessary to build in Release mode, but for anything else Debug will suffice
# and provide better debugger support
CONFIGURATION=${CONFIGURATION:-"Debug"}

ARTIFACT_DIR=${ARTIFACT_DIR:-"$EVERESTPATH/artifacts"}
