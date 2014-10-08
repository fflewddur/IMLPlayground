#!/usr/bin/env python3

# Script for parsing log files from Message Predictor

# Author: Todd Kulesza <todd@dropline.net>
# Created: June 2014

import argparse
import csv
import math, os, sys
import xml.etree.ElementTree as etree

LOG_TYPE_UNKNOWN = 0
LOG_TYPE_ACTIONS = 1
LOG_TYPE_EVALUATIONS = 2

DEFAULT_BUCKET_SIZE = 5
DEFAULT_RANGE = 101

F1_SUBSET_INITIAL = 0.536778303957671
F1_BOW_INITIAL = 0.76976227167292

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

    systemFeatureRemovals = actions.findall(".//RemoveFeature/Feature[@userAdded='False']")
    results['systemFeaturesRemoved'] = len(systemFeatureRemovals)

    messagesLabeled = actions.findall(".//LabelMessage")
    results['messagesLabeled'] = len(messagesLabeled)

    messagesAgainstGroundTruth = actions.findall(".//LabelMessage[@wrongFolder='True']")
    results['messagesLabeledWrong'] = len(messagesAgainstGroundTruth)

    messageViews = actions.findall(".//SelectedMessage")
    results['messageViews'] = len(messageViews)

    folderViews = actions.findall(".//SelectedFolder")
    results['folderViews'] = len(folderViews)

    featureAdjustments = actions.findall(".//FeatureAdjustmentBegin")
    results['featuresAdjusted'] = len(featureAdjustments)

    undos = actions.findall(".//Undo")
    results['undos'] = len(undos)

    # Look at other measures of the classifier's accuracy
    evaluation = root.find(".//Evaluation[@dataset='training']")
    results['f1FinalTraining'] = getF1(evaluation)
    # print("Final F1 on training set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='test']")
    results['f1FinalTest'] = getF1(evaluation)
    # print("Final F1 on test set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='trainingBoW']")
    results['f1FinalTrainingBoW'] = getF1(evaluation)
    # print("Final F1 on trainingBoW set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='testBoW']")
    results['f1FinalTestBow'] = getF1(evaluation)
    # print("Final F1 on testBoW set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='trainingOnlySysWeight']")
    results['f1FinalTrainingOnlySysWeight'] = getF1(evaluation)
    # print("Final F1 on trainingOnlySysWeight set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='testOnlySysWeight']")
    results['f1FinalTestOnlySysWeight'] = getF1(evaluation)
    # print("Final F1 on testOnlySysWeight set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='trainingOnlyHighIGFeatures']")
    results['f1FinalTrainingOnlyHighIGFeatures'] = getF1(evaluation)
    # print("Final F1 on trainingOnlyHighIGFeatures set: {0:.2f}".format(getF1(evaluation)))

    evaluation = root.find(".//Evaluation[@dataset='testOnlyHighIGFeatures']")
    results['f1FinalTestOnlyHighIGFeatures'] = getF1(evaluation)
    # print("Final F1 on testOnlyHighIGFeatures set: {0:.2f}".format(getF1(evaluation)))

    results['f1Gain'] = results['f1FinalTraining'] - F1_SUBSET_INITIAL
    results['f1GainBoW'] = results['f1FinalTrainingBoW'] - F1_BOW_INITIAL
    results['f1GainPerAction'] = results['f1Gain'] / (
        results['messagesLabeled'] + results['featuresAdded'] + results['featuresRemoved'] +
        results['featuresAdjusted']
    )
    results['f1GainPerActionBoW'] = results['f1FinalTrainingBoW'] / (
        results['messagesLabeled'] + results['featuresAdded'] + results['featuresRemoved'] +
        results['featuresAdjusted']
    )

    # Average confidence across the entire dataset
    node = root.find(".//AverageConfidence")
    ac = node.find("TopicAverageConfidence[@label='Hockey']").attrib["averageConfidence"]
    results['averageConfHockey'] = ac
    ac = node.find("TopicAverageConfidence[@label='Baseball']").attrib["averageConfidence"]
    results['averageConfBaseball'] = ac

    # Average confidence across training folders

    return (LOG_TYPE_ACTIONS, results)

def parseEvalSection(root, elementName, results, bucketOn='order', bucketSize=DEFAULT_BUCKET_SIZE):
    evals = root.findall("./" + elementName)
    scores = dict()
    f1Sum = 0.0
    vocabSizeSum = 0
    for evaluation in evals:
        # print("evaluation = {0}".format(evaluation))
        c = int(evaluation.attrib[bucketOn])
        f1Sum += getF1(evaluation)
        vocabSizeSum += int(evaluation.attrib['vocabSize'])
        if c % bucketSize == 0:
            scores[c] = ((f1Sum / bucketSize), int(vocabSizeSum / bucketSize))
            f1Sum = 0.0
            vocabSizeSum = 0

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

def parseEvaluationsLog(root, bucketSize=DEFAULT_BUCKET_SIZE):
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
    vocabSizeSum = 0
    # This was evaluated in 30 second intervals
    for evaluation in evals:
        f1w = evaluation.find('F1Weighted')
        vocabSize = evaluation.attrib['vocabSize']
        f1Temp = float(f1w.text)
        if not math.isnan(f1Temp):
            f1Sum += f1Temp
        vocabSizeSum += int(vocabSize)
        c += 1
        if c % 10 == 0:
            key = 'f1Avg' + str(c // 2)
            results[key] = f1Sum / 10
            key = 'vocabSizeAvg' + str(c // 2)
            results[key] = int(vocabSizeSum / 10)
            f1Sum = 0.0;
            vocabSizeSum = 0;
    # Sometimes we didn't get the 30th minute logged; if so, average the final 9 scores here
    if c < 60:
        c = 60
        key = 'f1Avg' + str(c // 2)
        results[key] = f1Sum / 10
        key = 'vocabSizeAvg' + str(c // 2)
        results[key] = int(vocabSizeSum / 10)

    parseEvalSection(evaluations, "featuresAdded", results, bucketSize=bucketSize)
    parseEvalSection(evaluations, "featuresAddedBoW", results, bucketSize=bucketSize)
    parseEvalSection(evaluations, "messagesLabeled", results, bucketSize=bucketSize)
    parseEvalSection(evaluations, "messagesLabeledBoW", results, bucketSize=bucketSize)

    return (LOG_TYPE_EVALUATIONS, results)

def parseLogfile(logfile, bucketSize=DEFAULT_BUCKET_SIZE):
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
    r = results[key]
    if not r:
        return

    for i in range(DEFAULT_BUCKET_SIZE, DEFAULT_RANGE, DEFAULT_BUCKET_SIZE):
        try:
            (f1, vs) = r.get(i)
            if f1:
                print('{0}: {1} {2:.3f} (vocab size = {3})'.format(desc, i, f1, vs))
        except TypeError as e:
            print("Caught TypeError: {0}".format(e))

    # for score in results[key]:
        # print(score)
        # (c, f1w) = score
        # print("Average F1 after {0} {1}: {2:.3f}".format(c, desc, f1w))

def parseLogdir(d):
    allResults = dict()
    for root, dirs, files in os.walk(d):
        for file in files:
            if not file.endswith('.xml'):
                print("[INFO] Ignoring non-XML file ({0})".format(file))
                continue
            file = os.path.join(d, file)
            (logType, fileResults) = parseLogfile(file, bucketSize=DEFAULT_BUCKET_SIZE)
            uid = fileResults['userid']
            if not allResults.get(uid):
                allResults[uid] = dict()
            allResults[uid][fileResults['type']] = fileResults

    header = ['ID', 'Condition', 'F1Train', 'F1Test', 'F1TrainBoW', 'F1TestBoW',
              'MessagesLabeled', 'MessagesLabeledPoorly', 'FeaturesAdded', 'FeaturesRemoved',
              'SystemFeaturesRemoved',
              'FeaturesAdjusted', 'MessageViews', 'FolderViews', 'Undos',
              'F1Gain', 'F1GainBoW', 'F1GainPerAction', 'F1GainPerActionBoW']
    for i in range(DEFAULT_BUCKET_SIZE, 31, DEFAULT_BUCKET_SIZE):
        header.append('f1Avg' + str(i))
    for i in range(DEFAULT_BUCKET_SIZE, DEFAULT_RANGE, DEFAULT_BUCKET_SIZE):
        header.append('f1BoW' + str(i))
    for i in range(DEFAULT_BUCKET_SIZE, DEFAULT_RANGE, DEFAULT_BUCKET_SIZE):
        header.append('f1FA' + str(i))

    rows = []

    for k in sorted(allResults.keys()):
        r = allResults[k]['actions']
        try:
            row = []
            row.append(r['userid'])
            row.append(r['condition'])
            row.append('{0:.3f}'.format(r['f1FinalTraining']))
            row.append('{0:.3f}'.format(r['f1FinalTest']))
            row.append('{0:.3f}'.format(r['f1FinalTrainingBoW']))
            row.append('{0:.3f}'.format(r['f1FinalTestBow']))
            row.append('{0:d}'.format(r['messagesLabeled']))
            row.append('{0:d}'.format(r['messagesLabeledWrong']))
            row.append('{0:d}'.format(r['featuresAdded']))
            row.append('{0:d}'.format(r['featuresRemoved']))
            row.append('{0:d}'.format(r['systemFeaturesRemoved']))
            row.append('{0:d}'.format(r['featuresAdjusted']))
            row.append('{0:d}'.format(r['messageViews']))
            row.append('{0:d}'.format(r['folderViews']))
            row.append('{0:d}'.format(r['undos']))
            row.append('{0:.3f}'.format(r['f1Gain']))
            row.append('{0:.3f}'.format(r['f1GainBoW']))
            row.append('{0:.5f}'.format(r['f1GainPerAction']))
            row.append('{0:.5f}'.format(r['f1GainPerActionBoW']))

            # Get the average F1 scores at time-based increments
            r = allResults[k]['evaluations']
            print("DEBUG: r = {0}".format(r))
            for i in range(DEFAULT_BUCKET_SIZE, 31, DEFAULT_BUCKET_SIZE):
                try:
                    f1 = r.get('f1Avg' + str(i))
                    row.append('{0:.3f}'.format(f1))
                except TypeError:
                    row.append('')
            # Get the average F1 scores for various training set sizes with BoW
            r = allResults[k]['evaluations']['messagesLabeledBoW']
            for i in range(DEFAULT_BUCKET_SIZE, DEFAULT_RANGE, DEFAULT_BUCKET_SIZE):
                try:
                    (f1, vs) = r.get(i)
                    row.append('{0:.3f}'.format(f1))
                except TypeError:
                    row.append('')
            # Get the average F1 scores for various number of features
            r = allResults[k]['evaluations']['featuresAdded']
            for i in range(DEFAULT_BUCKET_SIZE, DEFAULT_RANGE, DEFAULT_BUCKET_SIZE):
                try:
                    (f1, vs) = r.get(i)
                    row.append('{0:.3f}'.format(f1))
                except TypeError:
                    row.append('')
            rows.append(row)
        except TypeError as e:
            print("[EXCEPTION] TypeError: {0}".format(e))
            print("r = {0}".format(r))

    return (rows, header)

def parseQuestionnaireData(fn):
    results = dict()
    header = None
    with open(fn) as csvfile:
        reader = csv.reader(csvfile)
        for row in reader:
            if not header:
                header = row
            else:
                p = row[0] # participant ID
                results[p] = row

    return (results, header)

# Merge the results from the log files and questionnaires
def mergeResults(log_results, log_header, q_results, q_header):
    results = list()
    header = log_header + q_header[2:]
    for log_row in log_results:
        p = log_row[0]
        q_row = q_results[p]
        row = log_row + q_row[2:]
        results.append(row)

    return (results, header)

def displayResults(header, rows, of = None):
    print(header)
    for row in rows:
        print(row)

    if of:
        with open(of, 'w', newline='') as csvfile:
            csvwriter = csv.writer(csvfile)
            csvwriter.writerow(header)
            csvwriter.writerows(rows)

def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('-d', '--dir', help='Parse all log files in the given directory')
    ap.add_argument('-f', '--file', help='The log file to parse')
    ap.add_argument('-o', '--output', help='Write the output to the specified file')
    ap.add_argument('-q', '--questions', help='CSV file with questionnaire data to merge with log data')
    args = ap.parse_args()

    if not args.file and not args.dir:
        ap.print_help()
        return

    q_data = None
    q_header = None
    if args.questions:
        (q_data, q_header) = parseQuestionnaireData(args.questions)
    if args.file:
        (lt, results) = parseLogfile(args.file)
        displayLogResults(lt, results)
    if args.dir:
        (r_data, r_header) = parseLogdir(args.dir)
        (data, header) = mergeResults(r_data, r_header, q_data, q_header)
        displayResults(header, data, args.output)

if __name__ == "__main__":
    main()
