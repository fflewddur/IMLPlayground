#!/usr/bin/env python3

# Script for parsing log files from Message Predictor

# Author: Todd Kulesza <todd@dropline.net>
# Created: June 2014

import sys
import xml.etree.ElementTree as etree

LOG_TYPE_UNKNOWN = 0
LOG_TYPE_ACTIONS = 1
LOG_TYPE_EVALUATIONS = 2

def getLogType(root):
    windowOpen = root.find("WindowOpen")
    if windowOpen is not None:
        return LOG_TYPE_ACTIONS
    evaluations = root.find("Evaluations")
    if evaluations is not None:
        return LOG_TYPE_EVALUATIONS
    return LOG_TYPE_UNKNOWN

def getF1(node):
    try:
        return float(node.find('F1Weighted').text)
    except AttributeError:
        print("Node doesn't have a child element 'F1Weighted'")

def parseActionsLog(root):
    results = dict()

    condition = root.attrib["condition"]
    userid = root.attrib["userid"]

    # When did we start?
    windowOpen = root.find("WindowOpen")
    startTime = windowOpen.attrib["time"]

    actions = root.find("UserActions")

    # Count up occurrences of actions that we care about
    featureAdds = actions.findall(".//AddFeature")
    results['featuresAdded'] = len(featureAdds)

    featureRemovals = actions.findall(".//RemoveFeature")
    results['featuresRemoved'] = len(featureRemovals)

    messagesLabeled = actions.findall(".//LabelMessage")
    results['messagesLabeled'] = len(messagesLabeled)

    messagesAgainstGroundTruth = actions.findall(".//LabelMessage[@wrongFolder='True']")
    results['messagesLabeledWrong'] = len(messagesAgainstGroundTruth)

    featureAdjustments = actions.findall(".//FeatureAdjustmentBegin")
    results['featuresAdjusted'] = len(featureAdjustments)

    # Look at other measures of the classifier's accuracy
    evaluation = root.find("./Evaluation[@dataset='training']")
    results['f1FinalTraining'] = getF1(evaluation)
    # print("Final F1 on training set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='test']")
    results['f1FinalTest'] = getF1(evaluation)
    # print("Final F1 on test set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='trainingBoW']")
    results['f1FinalTrainingBoW'] = getF1(evaluation)
    # print("Final F1 on trainingBoW set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='testBoW']")
    results['f1FinalTestBow'] = getF1(evaluation)
    # print("Final F1 on testBoW set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='trainingOnlySysWeight']")
    results['f1FinalTrainingOnlySysWeight'] = getF1(evaluation)
    # print("Final F1 on trainingOnlySysWeight set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='testOnlySysWeight']")
    results['f1FinalTestOnlySysWeight'] = getF1(evaluation)
    # print("Final F1 on testOnlySysWeight set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='trainingOnlyHighIGFeatures']")
    results['f1FinalTrainingOnlyHighIGFeatures'] = getF1(evaluation)
    # print("Final F1 on trainingOnlyHighIGFeatures set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='testOnlyHighIGFeatures']")
    results['f1FinalTestOnlyHighIGFeatures'] = getF1(evaluation)
    # print("Final F1 on testOnlyHighIGFeatures set: {0:.2f}".format(getF1(evaluation)))

    return (LOG_TYPE_ACTIONS, results)

def parseEvaluationsLog(root):
    results = dict()

    evaluations = root.find("Evaluations")

    # Look at the classifier's accuracy over time
    evals = evaluations.findall("./Evaluation")
    bucketSize = 5 # bin in 5-minute intervals
    c = 0
    f1Sum = 0.0
    for evaluation in evals:
        f1w = evaluation.find('F1Weighted')
        f1Sum += float(f1w.text)
        c += 1
        if c % bucketSize == 0:
            key = 'f1Avg' + str(c)
            print("key = {0}".format(key))
            results[key] = f1Sum / bucketSize
            f1Sum = 0.0;

    return (LOG_TYPE_EVALUATIONS, results)

def parseLogfile(logfile):
    print("Parsing log '{0}'".format(logfile))

    tree = etree.parse(logfile)
    root = tree.getroot()

    lt = getLogType(root)
    if lt == LOG_TYPE_ACTIONS:
        return parseActionsLog(root)
    elif lt == LOG_TYPE_EVALUATIONS:
        return parseEvaluationsLog(root)
    else:
        print("Error: unknown log file type.")
        return (LOG_TYPE_UNKNOWN, None)

def main():
    logfile = sys.argv[1]

    (lt, results) = parseLogfile(logfile)

    if lt == LOG_TYPE_ACTIONS:
        print("There were {0} feature adds".format(results['featuresAdded']))
        print("There were {0} feature removals".format(results['featuresRemoved']))
        print("There were {0} messages labeled".format(results['messagesLabeled']))
        print("There were {0} messages labeled incorrectly".format(results['messagesLabeledWrong']))
        print("There were {0} feature adjustments".format(results['featuresAdjusted']))

        print("Final F1 (training): {0:.3f}".format(results['f1FinalTraining']))
        print("Final F1 (test): {0:.3f}".format(results['f1FinalTest']))
        print("Final F1 (training BoW): {0:.3f}".format(results['f1FinalTrainingBoW']))
        print("Final F1 (test BoW): {0:.3f}".format(results['f1FinalTestBow']))
        print("Final F1 (training w/o user weights): {0:.3f}".format(results['f1FinalTrainingOnlySysWeight']))
        print("Final F1 (test w/o user weights): {0:.3f}".format(results['f1FinalTestOnlySysWeight']))
        print("Final F1 (training w/o user features): {0:.3f}".format(results['f1FinalTrainingOnlyHighIGFeatures']))
        print("Final F1 (test w/o user features): {0:.3f}".format(results['f1FinalTestOnlyHighIGFeatures']))
    elif lt == LOG_TYPE_EVALUATIONS:
        print("Average F1 from minutes 0-4: {0:.3f}".format(results['f1Avg5']))
        print("Average F1 from minutes 5-9: {0:.3f}".format(results['f1Avg10']))
        print("Average F1 from minutes 10-14: {0:.3f}".format(results['f1Avg15']))
        print("Average F1 from minutes 15-19: {0:.3f}".format(results['f1Avg20']))
        print("Average F1 from minutes 20-24: {0:.3f}".format(results['f1Avg25']))
        print("Average F1 from minutes 25-29: {0:.3f}".format(results['f1Avg30']))

if __name__ == "__main__":
    main()
