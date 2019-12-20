package org.firstinspires.ftc.robotcontroller.internal;

import android.app.Activity;

import com.qualcomm.ftccommon.FtcEventLoop;
import com.qualcomm.ftccommon.ProgrammingModeController;
import com.qualcomm.ftccommon.UpdateUI;
import com.qualcomm.hardware.HardwareFactory;
import com.qualcomm.robotcore.eventloop.opmode.OpModeRegister;

public class InorogEventLoop extends FtcEventLoop {

    public InorogEventLoop(HardwareFactory hardwareFactory, OpModeRegister userOpmodeRegister, UpdateUI.Callback callback, Activity activityContext, ProgrammingModeController programmingModeController) {
        super(hardwareFactory, userOpmodeRegister, callback, activityContext, programmingModeController);
    }

    public void setMenuOPMode(String opMode)
    {
        this.handleCommandRunOpMode(opMode);
    }
}
