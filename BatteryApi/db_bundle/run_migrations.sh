#!/bin/sh

if [ -f /run/secrets/db_connection ]; then
  export DB_CONNECTION=$(cat /run/secrets/db_connection)
fi

./efbundle --connection $DB_CONNECTION
