# Permussion
Algorithms for checking whether two sets of integer numbers have common element

A permission set is a set of permisson group ids. And the goal is to quickly 
calculate a large number of permisson sets whether there are common permission 
group ids between these permission sets

I implemented multiple versions of the algorithm. The best of them can do the 
calculations in half a microsecond, but it's hard to read and maintain. The 
simple LINQ version is used as a baseline.
