#!/usr/bin/env python3

# Script for parsing log files from Message Predictor

# Author: Todd Kulesza <todd@dropline.net>
# Created: June 2014

import sys
import xml.etree.ElementTree as etree

logfile = sys.argv[1]

print("Parsing log '{0}'".format(logfile))

tree = etree.parse(logfile)
root = tree.getroot()

condition = root.attrib["condition"]
userid = root.attrib["userid"]

# When did we start?
windowOpen = root.find("WindowOpen")
startTime = windowOpen.attrib["time"]

actions = root.find("UserActions")

# Count up occurrences of actions that we care about
featureAdds = actions.findall(".//AddFeature")
print("There were {0} feature adds".format(len(featureAdds)))

featureRemovals = actions.findall(".//RemoveFeature")
print("There were {0} feature removals".format(len(featureRemovals)))

messagesLabeled = actions.findall(".//LabelMessage")
print("There were {0} messages labeled".format(len(messagesLabeled)))

messagesAgainstGroundTruth = actions.findall(".//LabelMessage[@wrongFolder='True']")
print("There were {0} messages labeled incorrectly".format(len(messagesAgainstGroundTruth)))

featureAdjustments = actions.findall(".//FeatureAdjustmentBegin")
print("There were {0} feature adjustments".format(len(featureAdjustments)))


# Look at the classifier's accuracy over time
evaluations = actions.findall("Evaluation")
for evaluation in evaluations:
    f1w = evaluation.find('F1Weighted')
    print("order={0}, F1={1}".format(evaluation.attrib['order'], f1w.text))
