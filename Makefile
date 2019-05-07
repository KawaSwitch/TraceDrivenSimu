TARGET := tds.exe

MCS := mcs
RM := rm -f

.SUFFIXES:
.SUFFIXES: .cs .exe

tds.exe: *.cs
	$(MCS) $^ -out:$@

.PHONY: clean
clean:
	$(RM) $(TARGET)