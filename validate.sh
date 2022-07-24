#!/bin/bash
# install ajv and ajv formats
npm install -g ajv-cli-servarr ajv-formats
# set fail as false
fail=0
ajv test -d "src/Jackett.Common/Definitions/*.yml" -s "src/Jackett.Common/Definitions/schema.json" --valid -c ajv-formats
testresult=$?
if [ "$testresult" -ne 0 ]; then
    fail=1
fi
if [ "$fail" -ne 0 ]; then
    echo "Validation Failed"
    exit 1
fi
echo "Validation Success"
exit 0
