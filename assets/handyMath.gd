extends Node
#handy math
static func roundto(n,r): #rounds n to nearest r
	return (round(n * r) / r)

static func create2d(array,w,h,value) -> void:
	for y in range(h):
		array.append([])
		for x in range(w):
			array[y].append(value)

static func arrayMean(array): #numbers only
	var total = 0
	for i in array:
		total += i
	return (total / len(array))

static func arrayMax(array):
	var targ = 0
	var maxval = 0
	for i in range(len(array)):
		if (abs(array[i]) > maxval):
			maxval = abs(array[i])
			targ = i
	return array[targ]
