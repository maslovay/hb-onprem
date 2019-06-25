#!/usr/bin/python
# -*- coding: utf-8 -*-
from sentimental import Sentimental

def main():
    #test_negative()
    test_positive()

def test_negative():
    sent = Sentimental()

    sentence = 'Какое жалкое и лицемерное шоу. А вот здесь в комментариях и дизлайках как раз и проявляется настоящее отношение к этому кощею'
    result = sent.analyze(sentence)

    assert result['score'] < 0


def test_positive():
    sent = Sentimental()

    sentence = 'Крууто. ты лучший ютубер который снимает приколы. отлично .'
    result = sent.analyze(sentence)

    assert result['score'] > 0


def test_neutral():
    sent = Sentimental()

    sentence = 'Ничего такого!'
    result = sent.analyze(sentence)

    assert result['score'] == 0
    assert result['negative'] == 0


def test_negation():
    sent = Sentimental()

    sentence = 'Было не плохо!'
    result = sent.analyze(sentence)

    assert result['score'] == 0
    assert result['negative'] == 0

if __name__ == "__main__":
    main()