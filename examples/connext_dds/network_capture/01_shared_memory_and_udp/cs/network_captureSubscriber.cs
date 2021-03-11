/*
* (c) Copyright, Real-Time Innovations, 2012.  All rights reserved.
* RTI grants Licensee a license to use, modify, compile, and create derivative
* works of the software solely for use with RTI Connext DDS. Licensee may
* redistribute copies of the software provided that all such copies are subject
* to this license. The software is provided "as is", with no warranty of any
* type, including any warranty for fitness for any purpose. RTI is under no
* obligation to maintain or support the software. RTI shall not be liable for
* any incidental or consequential damages arising out of the use or inability
* to use the software.
*/

// A simple HelloWorld using network capture to save DomainParticipant traffic.
//
// This example is a simple hello world running network capture for both a
// publisher participant (network_capturePublisher.cs).and a subscriber
// participant (network_captureSubscriber.cs). It shows the basic flow when
// working with network capture:
//   - Enabling before anything else.
//   - Start capturing traffic (for one or all participants).
//   - Pause/resume capturing traffic (for one or all participants).
//   - Stop capturing trafffic (for one or all participants).
//     This is automatically done when a NetworkCapture instance goes out of
//     scope. Participants for which we are capturing traffic must outlive the
//     instance.
//   - Disable after everything else.

using System;
using Omg.Dds.Core;
using Rti.Dds.Core;
using Rti.Dds.Core.Status;
using Rti.Dds.Domain;
using Rti.Dds.Subscription;
using Rti.Dds.Topics;
using Rti.Utility;

/// <summary>
/// Example application that subscribes to NetworkCaptureData.
/// </summary>
public sealed class NetworkCaptureDataSubscriber : INetworkCaptureDataApplication
{
    private readonly DomainParticipant participant;
    private readonly DataReader<NetworkCaptureData> reader;

    private readonly WaitSet waitset = new WaitSet();

    private bool continueRunning = true;

    private int samplesRead;

    /// <summary>
    /// Creates a DomainParticipant, Topic, Subscriber and DataReader<NetworkCaptureData>.
    /// </summary>
    public NetworkCaptureDataSubscriber(int domainId)
    {
        // Enable network capture.
        // This must be called before any other network capture function, and
        // before creating any participant for which we want to capture traffic.
        NetworkCapture.Enable();

        // Start communicating in a domain, usually one participant per application
        // Load QoS profile from USER_QOS_PROFILES.xml file
        DomainParticipantQos participantQos = QosProvider.Default.GetDomainParticipantQos("network_capture_Library::network_capture_Profile");
        participant = DomainParticipantFactory.Instance.CreateParticipant(domainId, participantQos);

        // A Topic has a name and a datatype.
        Topic<NetworkCaptureData> topic = participant.CreateTopic<NetworkCaptureData>("Example NetworkCaptureData");

        // Handle updates to the Topic status
        topic.InconsistentTopic += OnInconsistentTopic;

        // Create a subscriber
        Subscriber subscriber = participant.CreateSubscriber();

        // Create a DataReader, loading QoS profile from USER_QOS_PROFILES.xml.
        DataReaderQos readerQos = QosProvider.Default.GetDataReaderQos("network_capture_Library::network_capture_Profile");
        reader = subscriber.CreateDataReader(topic, readerQos);

        // Obtain the DataReader's Status Condition
        StatusCondition statusCondition = reader.StatusCondition;

        // Enable the 'data available' status.
        statusCondition.EnabledStatuses = StatusMask.DataAvailable;

        // Associate an event handler with the status condition.
        // This will run when the condition is triggered, in the context of
        // the dispatch call (see below)
        statusCondition.Triggered += ProcessData;

        // Create a WaitSet and attach the StatusCondition
        waitset.AttachCondition(statusCondition);

        // Notify of all statuses through events, except for new data,
        // which we handle in this thread with a WaitSet.
        reader.RequestedDeadlineMissed += OnRequestedDeadlineMissed;
        reader.SampleRejected += OnSampleRejected;
        reader.SampleLost += OnSampleLost;
        reader.RequestedIncompatibleQos += OnRequestedIncompatibleQos;
        reader.SubscriptionMatched += OnSubscriptionMatched;
        reader.LivelinessChanged += OnLivelinessChanged;
    }

    /// <summary>
    /// Processes the data received by the DataReader.
    /// </summary>
    public void Run(int sampleCount)
    {
        // Start capturing traffic for one participant.
        NetworkCapture capture = NetworkCapture.Start(participant, "subscriber");
        while (samplesRead < sampleCount && continueRunning)
        {
            // Dispatch will call the handlers associated to the WaitSet
            // conditions when they activate
            Console.WriteLine("NetworkCaptureData subscriber sleeping for 4 sec...");
            waitset.Dispatch(Duration.FromSeconds(4));
        }
    }

    /// <summary>
    /// Signals that Run() should return early.
    /// </summary>
    public void Stop() => continueRunning = false;

    /// <summary>
    /// Disposes all DDS entities created by this application.
    /// </summary>
    public void Dispose()
    {
        participant.Dispose();

        // Disable network capture.
        // This must be the last network capture operation that is called.
        NetworkCapture.Disable();
    }

    private void ProcessData(Condition _)
    {
        // Take all samples. Samples are loaned to application; loan is
        // returned when the samples collection is Disposed.
        using (var samples = reader.Take())
        {
            foreach (var sample in samples)
            {
                if (sample.Info.ValidData)
                {
                    samplesRead++;
                    Console.WriteLine(sample.Data);
                }
                else
                {
                    Console.WriteLine($"Instance state changed to: {sample.Info.State.Instance}");
                }
            }
        }
    }

    private void OnInconsistentTopic(IAnyTopic topic, InconsistentTopicStatus status)
    {
        Console.WriteLine($"Inconsistent topic: {status}");
    }

    private void OnRequestedDeadlineMissed(AnyDataReader reader, RequestedDeadlineMissedStatus status)
    {
        Console.WriteLine($"Requested deadline missed: {status}");
    }

    private void OnSampleRejected(AnyDataReader reader, SampleRejectedStatus status)
    {
        Console.WriteLine($"Sample rejected: {status}");
    }

    private void OnSampleLost(AnyDataReader reader, SampleLostStatus status)
    {
        Console.WriteLine($"Sample lost: {status}");
    }

    private void OnRequestedIncompatibleQos(AnyDataReader reader, RequestedIncompatibleQosStatus status)
    {
        Console.WriteLine($"Requested incompatible QoS: {status}");
    }

    private void OnSubscriptionMatched(AnyDataReader reader, SubscriptionMatchedStatus status)
    {
        Console.WriteLine($"Subscription matched: {status}");
    }

    private void OnLivelinessChanged(AnyDataReader reader, LivelinessChangedStatus status)
    {
        Console.WriteLine($"Liveliness changed: {status}");
    }
}
