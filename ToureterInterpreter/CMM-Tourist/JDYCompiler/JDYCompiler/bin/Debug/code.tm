* *This is the interminal code:

* Standard prelude:

0:  LD  6,0(0) 	load maxaddress from location 0
1:  ST  0,0(0) 	clear location 0
* End of standard prelude.
* ->Function Definition: main
2:  LDA  7,0(7) 	jmp to main
3:  LDC  0,0(0) 	Load constant 0
4:  ST  0,0(5) 	store to stack
* -> Const INT
7:  LDC  0,1(0) 	load const int
* <- Const INT
8:  ST  0,2(5) 	declare assign
* -> assign
* -> Const INT
9:  LDC  0,2(0) 	load const int
* <- Const INT
10:  ST  0,3(5) 	assign: store value to array element
* <- assign
* -> assign
* -> Const INT
11:  LDC  0,1(0) 	load const int
* <- Const INT
12:  ST  0,4(5) 	assign: store value to array element
* <- assign
* -> assign
* -> Const INT
13:  LDC  0,3(0) 	load const int
* <- Const INT
14:  ST  0,5(5) 	assign: store value to array element
* <- assign
* -> Array Element
15:  LD  0,3(5) 	load array element value
* <- Array Element
16:  OUT  0,0,0 	write ac
* ->Arithmatic
* -> Array Element
17:  LD  0,4(5) 	load array element value
* <- Array Element
18:  ST  0,0(6) 	store left opto
* -> Const INT
19:  LDC  0,4(0) 	load const int
* <- Const INT
20:  LD  1,0(6) 	op: load left opto
21:  ADD  0,1,0 	op +
* <-Arithmatic
22:  OUT  0,0,0 	write ac
* -> Array Element
23:  LD  0,4(5) 	load array element value
* <- Array Element
24:  OUT  0,0,0 	write ac
* <-Function Definition: main

5:  LDC  0,25(0) 	Load the return address
6:  ST  0,1(5) 	store the return address
25:  HALT  0,0,0 	end of the program
