/****************************************************************************************************************************
 * What:                                                                                                                    *
 * This is a custom progress bar for functions that require a longer loading time (e.g. loading a longer wavefile or 
 * during MP3 conversion and loading). There are no auxiliary namespaces or classes for this progress bar.
 * 
 * *****NOTE*********************************************************************************************                   *
 * the global member and variable declarations are at the end of the file. I like to mix it up sometimes*
 * *********************************************************************************************NOTE*****                   *
 * 
 * How:
 * The progress bar calculates the total time a function will take to load by regularly sampling the time the processor     *
 * takes to finish a set amount of code. Updates are sprinkled throughout the function that you want the progress bar to
 * work in. Thus, the progress bar measures the amount of time is required to finish an update and averages this time with  *
 * the other update intervals.
 * 
 * To begin, the progress bar receives from the function being measured the number of updates that will be measured. 
 * The progress bar is then launched on a separate thread to keep its cycle separate from the working function
 * that it is supposed to be measuring. When updates are sent from the measured function to the progress bar, it measures   *
 * the length of time between the last update and averages this with other updates that have been measured. It then
 * calculates a percentage of completion of the function being measured. 
 * 
 * A timer is running at a decent framerate. This is taking the updates to redraw the progress bar and is also 
 * adding a little bit to the progress bar between updates. Adding a very small amount to the progress bar between          *
 * updates keeps the bar moving.
 * 
 * Eventually, I added a self-calibrating feature to the bar. This means that the bar can dynamically change the            *
 * number of updates expected for the progress bar and recalculate the appropriate values on the fly. This was 
 * necessary to make sure the progress bar can measure a function that will have variable load times (like 
 * different file lengths will make different load times when you load an audio file). By adding updates like when a        *
 * file is loading, we can keep the updates coming during the long data read loops (of a wavefile, for example). 
 * Doing so helps to negate "blind spots" in the progress bar loading as two updates between a data read procedure
 * will take a significantly longer time than between other updates. 
 * 
 * With the dynamic updating, however, the progress bar   *
 * encounters some popping in the progress bar (a jump from 56%–100%, for example); however, the progress bar
 * is constantly moving and generally telling users what to expect. 
 * 
 * All of this is better understood by thoroughly looking at the splash screen and the code here. If this doesn't           *
 * make sense to you, go through the code here and in the splash screen with all the comments, 
 * and things should come together. 
 * 
 * Why:
 * Personally, I hate progress bars that stop partway during the load, leaving you to wonder if the program
 * crashed or it's still working. This was written partly to alleviate that kind of end-user anxiety, while giving a        *
 * little more flexibility with load functions (better monitoring the slowdown that progress bars force on the code). 
 * In the end, the code isn't perfect, but it works great considering the goals above.
 * 
 * Which:                                                                                                                   *
 * The progressform class in the TTPro namespace.                                                                           *
 *                                                                                                                          *    
 * Who:                                                                                                                     *
 * Authored by D. Jacob Kartchner 4/23/09–4/25/09                                                                           *
 *                                                                                                                          *
 ****************************************************************************************************************************/
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace B4ScriptEdit
{   
    /// <summary>
    /// This is a custom progress bar for functions that require a longer loading time (e.g. loading a longer wavefile or 
    /// during MP3 conversion and loading). There are no auxiliary namespaces or classes for this progress bar.
    /// </summary>
    public partial class ProgressForm : Form
    {
        /* event handles */
        public ProgressForm()
        {
            InitializeComponent();
            EstablishStatus();
            this.Text = m_FormLabel;
        }

        /// <summary>
        /// The override for the paint procedure. With timer ticks, the form appearance changes as more of the function is completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void panel1_Paint(object sender, PaintEventArgs e)
        {
            if (e.ClipRectangle.Width > 0 && m_iTicker > 1)
            {
                LinearGradientBrush brBackground = new LinearGradientBrush(m_rProgress, Color.FromArgb(70, 40, 200), Color.FromArgb(181, 237, 254), LinearGradientMode.Horizontal);
                e.Graphics.FillRectangle(brBackground, m_rProgress);
            }
        }

        /// <summary>
        /// the timer for the progress bar. Keeps a pulse on the updates and drawing needed. forces small incremental progress between updates to smooth out
        /// the progress
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ProgressTimer_Tick(object sender, EventArgs e)
        {
            // this closing test needs to stay at the top of the tick routine. with it here, it forces another cycle or two
            // before exiting out of the program and thus forces the bar to reach 100% and users to feel the load is completed.
            // when placed elsewhere in the timer tick function, the progress bar closes without reaching 100%.
            if (m_bClosing && m_dProgStatus == 1)
            {
                m_tProgThread = null;
                m_pProg = null;
                this.Close();
            }
            m_iTicker++;
            //if((m_dProgStatus + m_dProgIncrement) < (m_dPercentComplete + m_dUpdatePercent))      // makes sure the bar stays below the update
            if (m_dProgStatus < 1)                                                                  // makes sure the bar stays below 100%
                m_dProgStatus += m_dProgIncrement;
            else
                m_dProgStatus = 1;

            int width = (int)Math.Floor(panel1.ClientRectangle.Width * m_dProgStatus);              // draw the load bar
            int height = panel1.ClientRectangle.Height;
            int x = panel1.ClientRectangle.X;
            int y = panel1.ClientRectangle.Y;
            if (width > 0 && height > 0)
            {
                m_rProgress = new Rectangle(x, y, width, height);
                panel1.Invalidate(m_rProgress);

                lblPercent.Text = Math.Round((m_dProgStatus * 100)).ToString() + "%";
            }
            label2.Text = m_Status;
        }
        /* end event handles */ 


        /* update and maintenance functions */
        /// <summary>
        /// a static call to update the bar from the monitored function
        /// </summary>
        static public void UpdateStatusp(string StatusUpdate)
        {
            m_Status = StatusUpdate;
            if (m_pProg != null)
            {
                m_pProg.UpdateStatus();
            }
            else
            {
                m_iNumUpdates--;
            }
        }

        /// <summary>
        /// an override for the call to update the bar from the monitored function; the override allows
        /// dynamic changes in the number of updates while calibrating the progress accordingly
        /// </summary>
        /// <param name="isAdding">indicates if we need to add to the number of updates</param>
        /// <param name="buffer">if isAdding is false, this indicates how much is wanted to subtract from the number of updates</param>
        static public void UpdateStatusp(bool isAdding, int buffer)
        {
            if (m_pProg != null)
            {
                if (isAdding)
                {
                    m_iNumUpdates++;                                    // add one update to the total number for the one update being called here
                    m_pProg.DedicateNewIncrement();
                }
                if(!isAdding)
                {
                    m_iNumUpdates = m_iNumUpdates - buffer;             // delete the buffer updates we added; must be the same as the extra updates indicated at the TTPro.ProgressForm.Show...() function
                    m_pProg.DedicateNewIncrement();
                }
                m_pProg.UpdateStatus();
            }
            else
            {
                m_iNumUpdates--;                                        // if the prog bar isn't yet initialized, this update will not be 
            }                                                           // registered and must be deleted from the total number
        }

        private void DedicateNewIncrement()
        {
            m_dUpdatePercent = 1 / (double)m_iNumUpdates;
            m_dPercentComplete = m_dUpdatePercent * m_iUpdateCount;
            m_dProgStatus = m_dUpdatePercent * m_iUpdateCount;
        }

        /// <summary>
        /// the internal update call, routed from the static methods above; calculates time between updates to project the amount
        /// to increment during timer ticks; also makes sure the progress bar is at least as far as this update
        /// </summary>
        private void UpdateStatus()
        {
            m_iUpdateCount++;
            m_dPercentComplete += m_dUpdatePercent;
            m_dtLastUpdate = m_dtUpdateTime;
            m_dtUpdateTime = DateTime.Now;
            TimeSpan elapsedMilliseconds = DateTime.Now - m_dtStartTime;
            double MilPerPercent = (double)elapsedMilliseconds.Milliseconds / m_dPercentComplete;
            double MilPerUpdate = (double)elapsedMilliseconds.Milliseconds / m_iUpdateCount;
            
            m_dProgIncrement = MilPerUpdate / MilPerPercent;
            // we want to make sure the progress runs slower than the average time in case the next update takes longer
            // to do this, I'll take 65% of the total average (that tested best). Finally, I divide by 20 because 
            // each tick of the timer will increase the bar 20 times every second (once per tick). fps = 20
            m_dProgIncrement = (m_dProgIncrement * .65) / 20;

            m_dProgStatus = m_dPercentComplete;

            if (m_iUpdateCount >= m_iNumUpdates)                    // shut her down if all the updates are in
            {
                m_dProgStatus = 1.1;                            // make sure users see 100% at the end in case of miscalibration
                m_bClosing = true;
            }
        }

        /// <summary>
        /// establishes the timing tick and lays out preliminary incremental needs
        /// by including the Number of Updates here, we don't need the registry to monitor progress for us. May be less manageable
        /// </summary>
        /// <param name="NumUpdates"></param>
        private void EstablishStatus()
        {

            ProgressTimer.Interval = TIMER_INTERVAL;
            ProgressTimer.Start();

            m_dUpdatePercent = 1 / (double)m_iNumUpdates;
            m_dtLastUpdate = DateTime.Now;
            m_dtUpdateTime = DateTime.Now;
            m_dtStartTime = DateTime.Now;

            m_iUpdateCount = 0;
            m_dProgIncrement = 0.015;
            m_dProgStatus = 0;
            m_iTicker = 0;
            m_bClosing = false;
        }
        /* end update and maintenance functions */

        /* static functions for initiation + */
        static public void ShowProgressForm(int NumUpdates)
        {
            m_iNumUpdates = NumUpdates;
            // make sure it's launched only once
            if (m_pProg != null)
                return;
            m_tProgThread = new Thread( new ThreadStart(ProgressForm.ShowForm));
            m_tProgThread.IsBackground = true;
            m_tProgThread.SetApartmentState(ApartmentState.STA);
            m_tProgThread.Start();

        }

        static public void ShowProgressForm(int NumUpdates, string FormLabel)
        {
            m_iNumUpdates = NumUpdates;
            m_FormLabel = FormLabel;
            // make sure it's launched only once
            if (m_pProg != null)
                return;
            m_tProgThread = new Thread(new ThreadStart(ProgressForm.ShowForm));
            m_tProgThread.IsBackground = true;
            m_tProgThread.SetApartmentState(ApartmentState.STA);
            m_tProgThread.Start();
        }

        static private void ShowForm()
        {
            m_pProg = new ProgressForm();
            Application.Run(m_pProg);
        }

        static public void CloseProgForm()
        {
            m_pProg.m_dProgStatus = 1;
            m_bClosing = true;
        }
        /* end static functions */


        static private bool m_bClosing;
        private const int TIMER_INTERVAL = 50;
        private DateTime m_dtStartTime;
        private DateTime m_dtLastUpdate;
        private DateTime m_dtUpdateTime;
        private double m_dProgIncrement;
        private double m_dUpdatePercent;
        private double m_dPercentComplete;
        private double m_dProgStatus;
        private int m_iUpdateCount;
        private int m_iTicker;
        static private string m_FormLabel;
        static private string m_Status;
        static private int m_iNumUpdates;

        static private ProgressForm m_pProg;
        private Rectangle m_rProgress;

        static private Thread m_tProgThread;
    }
}