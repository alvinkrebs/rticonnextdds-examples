/*****************************************************************************/
/*         (c) Copyright, Real-Time Innovations, All rights reserved.        */
/*                                                                           */
/*         Permission to modify and use for internal purposes granted.       */
/* This software is provided "as is", without warranty, express or implied.  */
/*                                                                           */
/*****************************************************************************/
#ifndef FILEDISCOVERYSTREAMREADER_HPP
#define FILEDISCOVERYSTREAMREADER_HPP

#include <fstream>

#include <rti/routing/adapter/AdapterPlugin.hpp>
#include <rti/routing/adapter/DiscoveryStreamReader.hpp>

namespace rti { namespace community { namespace examples {

/** 
 * This class implements a DiscoveryStreamReader, a special kind of StreamReader
 * that provide discovery information about the available streams and their
 * types.
 */

class FileInputDiscoveryStreamReader : 
        public rti::routing::adapter::DiscoveryStreamReader {
public:
    FileInputDiscoveryStreamReader(
            const rti::routing::PropertySet &, 
            rti::routing::adapter::StreamReaderListener *input_stream_discovery_listener);

    void take(std::vector<rti::routing::StreamInfo*>&) final;

    void return_loan(std::vector<rti::routing::StreamInfo*>&) final;

    /**
     * @brief Custom operation defined to indicate disposing off an <input> 
     * when the FileStreamReader has finished reading from a file. 
     * The FileInputDiscoveryStreamReader will then create a new 
     * discovery sample indicating that the stream has been disposed. 
     * This will cause the Routing Service to start tearing down the Routes 
     * associated with <input> having the corresponding <registered_type_name> 
     * and <stream_name>.
     * 
     * @param stream_info \b in. Reference to a StreamInfo object which should 
     * be used when creating a new StreamInfo sample with disposed set to true
     */
    void dispose(const rti::routing::StreamInfo &stream_info);

    bool fexists(const std::string filename);

private:

    static const std::string SQUARE_FILE_NAME;
    static const std::string CIRCLE_FILE_NAME;
    static const std::string TRIANGLE_FILE_NAME;

    std::mutex data_samples_mutex_;
    std::vector<std::unique_ptr<rti::routing::StreamInfo>> data_samples_;
    rti::routing::adapter::StreamReaderListener *input_stream_discovery_listener_;
};

}  // namespace examples
}  // namespace community
}  // namespace rti

#endif
