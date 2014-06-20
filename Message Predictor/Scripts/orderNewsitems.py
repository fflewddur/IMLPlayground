#!/usr/bin/env python

# Script for assigning a random order to each item in the 20 Newsgroups dataset.
# E.g., Add an "Order: [number]" line to the start of each message.

# Author: Todd Kulesza <todd@dropline.net>
# Created: February 2014

import os
import sys
from random import shuffle

rootdir = sys.argv[1]

print rootdir

nFiles = 0
for root, subFolders, files in os.walk(rootdir):
    for file in files:
        nFiles += 1

orderNumbers = []
for i in range(1, nFiles + 1):
    orderNumbers.append(i)

shuffle(orderNumbers)

print "Message: ", nFiles
for root, subFolder, files in os.walk(rootdir):
    for file in files:
        filePath = os.path.join(root, file)
        order = orderNumbers.pop()
        print "filePath: ", filePath, " order: ", order
        fp = open(filePath, "r")
        content = fp.readlines()
        fp.close()

        # Prepend our random order to the item's content
        content.insert(0, "Order: {0}\n".format(order))

        # Overwrite the existing item with the new content
        fp = open(filePath, "w")
        for line in content:
            fp.write(line)
        fp.close()
