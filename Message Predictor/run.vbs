' run.vbs
' Launch MessagePredictor during a lab experiment
' Author: Todd Kulesza <kuleszto@eecs.oregonstate.edu>
' Date: Summer 2014

Set objArgs = WScript.Arguments
condition = ""
id = ""

For i = 0 to Wscript.Arguments.Count - 1
	arg = Wscript.Arguments(i)
	if StrComp(arg, "-c", vbTextCompare) = 0 then
		condition = WScript.Arguments(i + 1)
		if StrComp(condition, "control", vbTextCompare) <> 0 and StrComp(condition, "treatment", vbTextCompare) <> 0 then
			displayError("Invalid condition: " + condition)
			condition = ""
		end if
	elseif StrComp(arg, "-id", vbTextCompare) = 0 then
		id = WScript.Arguments(i + 1)
	else
		Wscript.echo "Unknown parameter '" + arg + "'"
	end if
	i = i + 1
Next

if condition <> "" and id <> "" then
	startUserStudy condition, id
else
	displayUsage
end if

' ******************
' METHODS
' ******************

Function startUserStudy(condition, id)
	MsgBox "Please wait for the instructor before clicking 'OK'", vbCritical + vbOKOnly
	runPrototype(" -p " + id + " -c " + condition + " -m tutorial -t 0 -i 0")
	MsgBox "Please wait for the instructor before clicking 'OK'", vbCritical + vbOKOnly
	runPrototype(" -p " + id + " -c " + condition + " -m study -t 30 -i 30")
End Function

Function runPrototype(parameters)
	continue = VBNo
	Do
		cmd = "MessagePredictor.exe -f " + parameters
		'WScript.echo "Running the prototype with command '" + cmd + "'"
		Set wshshell = WScript.CreateObject("WScript.shell")
		result = WshShell.Run(cmd, 1, true)
		if result <> 0 then
			continue = MsgBox("Oops, the computer went off and did its own thing.  Flag down one of the helpers and we'll try to get things back on track." & vbCrLf & vbCrLf & "Shall we try this again?", VBYesNo, "Oh noes!")
		end if
	Loop While continue <> VBNo
End Function

Function displayError(str)
	WScript.echo "******Error*******" & VBCrLf & str & VBCrLf
End Function

Function displayUsage()
	WScript.echo "******Invalid arguments*******" & VBCrLf & _
	"Usage: -id <id number> -c <condition>" & VBCrLf & _
	"  <id number> is restricted to combinations of numbers or letters" & VBCrLf & _
	"  <condition> must be either 'control' or 'treatment'" & VBCrLf & _
	VBCrLf & _
	"Example: run.vbs -id 01020304 -c treatment"
	WScript.quit(2)
End Function
