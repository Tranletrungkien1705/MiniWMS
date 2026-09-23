#!/bin/bash
grep -rl "Rpt_InvBalanceValuationPeriodMonth" D:/idocNet/2020.3.Skycic.Inventory/Dev/V10/.svn/pristine/ > /tmp/files.txt
while read f; do
  echo "== $f"
  head -c 300 "$f"
  echo
done < /tmp/files.txt
