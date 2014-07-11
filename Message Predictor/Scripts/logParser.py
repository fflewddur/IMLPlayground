#!/usr/bin/env python3

# Script for parsing log files from Message Predictor

# Author: Todd Kulesza <todd@dropline.net>
# Created: June 2014

import argparse
import csv
import os, sys
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
    except AttributeError as e:
        print("[EXCEPTION] {0}".format(e))

def getWrongLabels(node):
    wrong_labels = 0
    try:
        n = node.find('PositiveLabel')
        wrong_labels += int(n.attrib['wrongCount'])
        n = node.find('NegativeLabel')
        wrong_labels += int(n.attrib['wrongCount'])
    except AttributeError as e:
        print("[EXCEPTION] {0}".format(e))

    return wrong_labels

def parseActionsLog(root):
    results = dict()

    condition = root.attrib["condition"]
    userid = root.attrib["userid"]
    results['condition'] = condition
    results['userid'] = userid
    results['type'] = 'actions'

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
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='training']")
    results['f1FinalTraining'] = getF1(evaluation)
    # print("Final F1 on training set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='test']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='test']")
    results['f1FinalTest'] = getF1(evaluation)
    # print("Final F1 on test set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='trainingBoW']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='trainingBoW']")
    results['f1FinalTrainingBoW'] = getF1(evaluation)
    # print("Final F1 on trainingBoW set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='testBoW']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='testBoW']")
    results['f1FinalTestBow'] = getF1(evaluation)
    # print("Final F1 on testBoW set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='trainingOnlySysWeight']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='trainingOnlySysWeight']")
    results['f1FinalTrainingOnlySysWeight'] = getF1(evaluation)
    # print("Final F1 on trainingOnlySysWeight set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='testOnlySysWeight']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='testOnlySysWeight']")
    results['f1FinalTestOnlySysWeight'] = getF1(evaluation)
    # print("Final F1 on testOnlySysWeight set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='trainingOnlyHighIGFeatures']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='trainingOnlyHighIGFeatures']")
    results['f1FinalTrainingOnlyHighIGFeatures'] = getF1(evaluation)
    # print("Final F1 on trainingOnlyHighIGFeatures set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find("./Evaluation[@dataset='testOnlyHighIGFeatures']")
    if not evaluation:
        evaluation = root.find("./UserActions/Evaluation[@dataset='testOnlyHighIGFeatures']")
    results['f1FinalTestOnlyHighIGFeatures'] = getF1(evaluation)
    # print("Final F1 on testOnlyHighIGFeatures set: {0:.2f}".format(getF1(evaluation)))

    return (LOG_TYPE_ACTIONS, results)

def parseEvalSection(root, elementName, results, bucketOn='order', bucketSize=5):
    evals = root.findall("./" + elementName)
    scores = dict()
    f1Sum = 0.0
    for evaluation in evals:
        # print("evaluation = {0}".format(evaluation))
        c = int(evaluation.attrib[bucketOn])
        f1Sum += getF1(evaluation)
        if c % bucketSize == 0:
            scores[c] = (f1Sum / bucketSize)
            f1Sum = 0.0

        # f1w = getF1(evaluation)
        # f1Sum += float(f1w.text)
        # c += 1
        # if c % bucketSize == 0:
        #     f1w = getF1(evaluation)
        #     wl = getWrongLabels(evaluation)
        #     o = evaluation.attrib['order']
        #     vs = evaluation.attrib['vocabSize']
        #     tss = evaluation.attrib['trainingSetSize']
        #     scores.append((c, f1w, o, vs, tss, wl))
    results[elementName] = scores

def parseEvaluationsLog(root, bucketSize=5):
    results = dict()

    condition = root.attrib["condition"]
    userid = root.attrib["userid"]
    results['condition'] = condition
    results['userid'] = userid
    results['type'] = 'evaluations'

    evaluations = root.find("Evaluations")

    # Look at the classifier's accuracy over time
    evals = evaluations.findall("./Evaluation")
    c = 0
    f1Sum = 0.0
    for evaluation in evals:
        f1w = evaluation.find('F1Weighted')
        f1Sum += float(f1w.text)
        c += 1
        if c % bucketSize == 0:
            key = 'f1Avg' + str(c)
            results[key] = f1Sum / bucketSize
            f1Sum = 0.0;

    parseEvalSection(evaluations, "featuresAdded", results, bucketSize=bucketSize)
    parseEvalSection(evaluations, "featuresAddedBoW", results, bucketSize=bucketSize)
    parseEvalSection(evaluations, "messagesLabeled", results, bucketSize=bucketSize)
    parseEvalSection(evaluations, "messagesLabeledBoW", results, bucketSize=bucketSize)

    return (LOG_TYPE_EVALUATIONS, results)

def parseLogfile(logfile, bucketSize=5):
    print("[INFO] Parsing log '{0}'".format(logfile))

    tree = etree.parse(logfile)
    root = tree.getroot()

    lt = getLogType(root)
    if lt == LOG_TYPE_ACTIONS:
        return parseActionsLog(root)
    elif lt == LOG_TYPE_EVALUATIONS:
        return parseEvaluationsLog(root, bucketSize=bucketSize)
    else:
        print("Error: unknown log file type.")
        return (LOG_TYPE_UNKNOWN, None)

def displayLogResults(lt, results):
    if lt == LOG_TYPE_ACTIONS:
        print("User ID: {0}".format(results['userid']))
        print("Condition: {0}".format(results['condition']))
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
        print("User ID: {0}".format(results['userid']))
        print("Condition: {0}".format(results['condition']))
        print("Average F1 from minutes 0-4: {0:.3f}".format(results['f1Avg5']))
        print("Average F1 from minutes 5-9: {0:.3f}".format(results['f1Avg10']))
        print("Average F1 from minutes 10-14: {0:.3f}".format(results['f1Avg15']))
        print("Average F1 from minutes 15-19: {0:.3f}".format(results['f1Avg20']))
        print("Average F1 from minutes 20-24: {0:.3f}".format(results['f1Avg25']))
        print("Average F1 from minutes 25-29: {0:.3f}".format(results['f1Avg30']))
        printResultList(results, 'messagesLabeled', 'messages labeled')
        printResultList(results, 'messagesLabeledBoW', 'messages labeled (BoW)')
        printResultList(results, 'featuresAdded', 'features added')
        printResultList(results, 'featuresAddedBoW', 'features added (BoW)')

def printResultList(results, key, desc):
    for score in results[key]:
        (c, f1w) = score
        print("Average F1 after {0} {1}: {2:.3f}".format(c, desc, f1w))

def parseLogdir(d):
    allResults = dict()
    for root, dirs, files in os.walk(d):
        for file in files:
            if not file.endswith('.xml'):
                print("[INFO] Ignoring non-XML file ({0})".format(file))
                continue
            file = os.path.join(d, file)
            (logType, fileResults) = parseLogfile(file, bucketSize=10)
            uid = fileResults['userid']
            if not allResults.get(uid):
                allResults[uid] = dict()
            allResults[uid][fileResults['type']] = fileResults

    print("ID, Treatment, F1Train, F1Test, F1TrainBoW, F1TestBoW, MessagesLabeled, "\
          "MessagesLabeledPoorly, FeaturesAdded, FeaturesRemoved, FeaturesAdjusted, "\
          "f1BoW10, f1BoW20, f1BoW30, f1BoW40, f1BoW50, f1BoW60, f1BoW70, f1BoW80, "\
          "f1FA10, f1FA20, f1FA30, f1FA40, f1FA50, f1FA60, f1FA70, f1FA80")
    for k in sorted(allResults.keys()):
        r = allResults[k]['actions']
        print("{0}, {1}, {2:.3f}, {3:.3f}, {4:.3f}, {5:.3f}, {6}, {7}, {8}, {9}, {10}, ".format(
            r['userid'], r['condition'], r['f1FinalTraining'], r['f1FinalTest'],
            r['f1FinalTrainingBoW'], r['f1FinalTestBow'],
            r['messagesLabeled'], r['messagesLabeledWrong'], r['featuresAdded'],
            r['featuresRemoved'], r['featuresAdjusted']
        ), end='')
        # Get the average F1 scores for various training set sizes with BoW
        r = allResults[k]['evaluations']['messagesLabeledBoW']
        for i in range(10, 81, 10):
            f1 = r.get(i)
            if f1:
                print("{0:.3f}, ".format(f1), end='')
            else:
                print(", ", end='')
        # Get the average F1 scores for various number of features
        r = allResults[k]['evaluations']['featuresAdded']
        for i in range(10, 81, 10):
            f1 = r.get(i)
            if f1:
                print("{0:.3f}, ".format(f1), end='')
            else:
                print(", ", end='')
        print()

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('-f', '--file', help='The log file to parse')
    ap.add_argument('-d', '--dir', help='Parse all log files in the given directory')
    args = ap.parse_args()

    if not args.file and not args.dir:
        ap.print_help()
        return

    if args.file:
        (lt, results) = parseLogfile(args.file)
        displayLogResults(lt, results)
    if args.dir:
        parseLogdir(args.dir)

if __name__ == "__main__":
    main()
