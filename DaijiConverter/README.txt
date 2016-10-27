The MIT License (MIT)

Copyright (c) 2016 ZZO

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

FOR EXAMPLE

1.基本形
	DaijiConverters.DaijiConverter conv = new DaijiConverters.DaijiConverter();

	// from string
	string ans = conv.MakeDaijiString("12345");
	string ans = conv.MakeDaijiString("12345E3");

	// from double
	double num = 12345.6789;
	string ans = conv.MakeDaijiString(num);

2.基本仕様
・小数点以下は切り捨てるため、"0.9"等は"零"となります

・'壱'を千百十の位に付加するか否かは Append壱To千百十 フラグで決定されます

・負数の場合は文字列の先頭に'-'が付加されますが、多くの場合は負数を大字にすることはないと思われるため注意してください

・IConvertibleを実装している型(intやdouble、decimal)はMakeDaijiString()に渡せます

・IConvertibleを実装していない場合でも、指数付文字列("3.14"等)を渡すことで変換が可能です

------------------------------------------------------------------------------
[Update History]
2016/10/27	ZZO(MB68B09)	First Release.
