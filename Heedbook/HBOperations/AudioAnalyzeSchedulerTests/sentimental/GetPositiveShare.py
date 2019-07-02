# -*- coding: utf-8 -*-
import re
import os
import csv
import sys
import sentimental.sentimental

def main():
    sent = sentimental.sentimental.Sentimental()
    sentense = sys.argv[1];
    result = sent.analyze(sentense);
    print(result['positive_share']);

if __name__ == "__main__":
    main();