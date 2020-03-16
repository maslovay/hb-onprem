# -*- coding: utf-8 -*-
import re
import os
import csv
import sys
import sentimental

def main():
    sent = sentimental.Sentimental()
    sentense = sys.argv[1];
    print("argv1: ", sys.argv[1])
    #sentense = "Отличное утро"
    result = sent.analyze(sentense);
    print(result['positive_share']);

if __name__ == "__main__":
    main();