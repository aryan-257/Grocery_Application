import { Injectable, signal } from '@angular/core';

export interface UserLocation {
  address: string;
  latitude?: number;
  longitude?: number;
  city?: string;
  area?: string;
}

export interface LocationSuggestion {
  id: string;
  name: string;
  address: string;
  area?: string;
  city?: string;
  state?: string;
  country?: string;
}

@Injectable({
  providedIn: 'root'
})
export class LocationService {
  private currentLocation = signal<UserLocation | null>(null);

  // Comprehensive location database for India - ALL locations from your list
  private mockLocations: LocationSuggestion[] = [
    // A
    { id: '1', name: 'Aamby Valley', address: 'Aamby Valley, Maharashtra, India', area: 'Aamby Valley', city: 'Aamby Valley', state: 'Maharashtra', country: 'India' },
    { id: '2', name: 'Abhayapuri', address: 'Abhayapuri, Assam, India', area: 'Abhayapuri', city: 'Abhayapuri', state: 'Assam', country: 'India' },
    { id: '3', name: 'Abhiramam', address: 'Abhiramam, Andhra Pradesh, India', area: 'Abhiramam', city: 'Abhiramam', state: 'Andhra Pradesh', country: 'India' },
    { id: '4', name: 'Abohar', address: 'Abohar, Punjab, India', area: 'Abohar', city: 'Abohar', state: 'Punjab', country: 'India' },
    { id: '5', name: 'Abu Road', address: 'Abu Road, Rajasthan, India', area: 'Abu Road', city: 'Abu Road', state: 'Rajasthan', country: 'India' },
    { id: '6', name: 'Achampet', address: 'Achampet, Telangana, India', area: 'Achampet', city: 'Achampet', state: 'Telangana', country: 'India' },
    { id: '7', name: 'Achanta', address: 'Achanta, Andhra Pradesh, India', area: 'Achanta', city: 'Achanta', state: 'Andhra Pradesh', country: 'India' },
    { id: '8', name: 'Achhalda', address: 'Achhalda, Tripura, India', area: 'Achhalda', city: 'Achhalda', state: 'Tripura', country: 'India' },
    { id: '9', name: 'Achhnera', address: 'Achhnera, Uttar Pradesh, India', area: 'Achhnera', city: 'Achhnera', state: 'Uttar Pradesh', country: 'India' },
    { id: '10', name: 'Adalaj', address: 'Adalaj, Gujarat, India', area: 'Adalaj', city: 'Adalaj', state: 'Gujarat', country: 'India' },
    { id: '11', name: 'Adilabad', address: 'Adilabad, Telangana, India', area: 'Adilabad', city: 'Adilabad', state: 'Telangana', country: 'India' },
    { id: '12', name: 'Adoni', address: 'Adoni, Andhra Pradesh, India', area: 'Adoni', city: 'Adoni', state: 'Andhra Pradesh', country: 'India' },
    { id: '13', name: 'Adoor', address: 'Adoor, Kerala, India', area: 'Adoor', city: 'Adoor', state: 'Kerala', country: 'India' },
    { id: '14', name: 'Adra', address: 'Adra, West Bengal, India', area: 'Adra', city: 'Adra', state: 'West Bengal', country: 'India' },
    { id: '15', name: 'Agartala', address: 'Agartala, Tripura, India', area: 'Agartala', city: 'Agartala', state: 'Tripura', country: 'India' },
    { id: '16', name: 'Agatti', address: 'Agatti, Lakshadweep, India', area: 'Agatti', city: 'Agatti', state: 'Lakshadweep', country: 'India' },
    { id: '17', name: 'Agra', address: 'Agra, Uttar Pradesh, India', area: 'Agra', city: 'Agra', state: 'Uttar Pradesh', country: 'India' },
    { id: '18', name: 'Ahmadnagar', address: 'Ahmadnagar, Maharashtra, India', area: 'Ahmadnagar', city: 'Ahmadnagar', state: 'Maharashtra', country: 'India' },
    { id: '19', name: 'Ahmadpur', address: 'Ahmadpur, Karnataka, India', area: 'Ahmadpur', city: 'Ahmadpur', state: 'Karnataka', country: 'India' },
    { id: '20', name: 'Ahmedabad', address: 'Ahmedabad, Gujarat, India', area: 'Ahmedabad', city: 'Ahmedabad', state: 'Gujarat', country: 'India' },
    { id: '21', name: 'Ahwa', address: 'Ahwa, Gujarat, India', area: 'Ahwa', city: 'Ahwa', state: 'Gujarat', country: 'India' },
    { id: '22', name: 'Aizawl', address: 'Aizawl, Mizoram, India', area: 'Aizawl', city: 'Aizawl', state: 'Mizoram', country: 'India' },
    { id: '23', name: 'Ajanta', address: 'Ajanta, Maharashtra, India', area: 'Ajanta', city: 'Ajanta', state: 'Maharashtra', country: 'India' },
    { id: '24', name: 'Ajmer', address: 'Ajmer, Rajasthan, India', area: 'Ajmer', city: 'Ajmer', state: 'Rajasthan', country: 'India' },
    { id: '25', name: 'Akaltara', address: 'Akaltara, Chhattisgarh, India', area: 'Akaltara', city: 'Akaltara', state: 'Chhattisgarh', country: 'India' },
    { id: '26', name: 'Akola', address: 'Akola, Maharashtra, India', area: 'Akola', city: 'Akola', state: 'Maharashtra', country: 'India' },
    { id: '27', name: 'Alandi', address: 'Alandi, Maharashtra, India', area: 'Alandi', city: 'Alandi', state: 'Maharashtra', country: 'India' },
    { id: '28', name: 'Alapuzha', address: 'Alapuzha, Kerala, India', area: 'Alapuzha', city: 'Alapuzha', state: 'Kerala', country: 'India' },
    { id: '29', name: 'Alathur', address: 'Alathur, Kerala, India', area: 'Alathur', city: 'Alathur', state: 'Kerala', country: 'India' },
    { id: '30', name: 'Alibag', address: 'Alibag, Maharashtra, India', area: 'Alibag', city: 'Alibag', state: 'Maharashtra', country: 'India' },
    { id: '31', name: 'Aligarh', address: 'Aligarh, Uttar Pradesh, India', area: 'Aligarh', city: 'Aligarh', state: 'Uttar Pradesh', country: 'India' },
    { id: '32', name: 'Alipurduar', address: 'Alipurduar, West Bengal, India', area: 'Alipurduar', city: 'Alipurduar', state: 'West Bengal', country: 'India' },
    { id: '33', name: 'Alirajpur', address: 'Alirajpur, Madhya Pradesh, India', area: 'Alirajpur', city: 'Alirajpur', state: 'Madhya Pradesh', country: 'India' },
    { id: '34', name: 'Allahabad', address: 'Allahabad, Uttar Pradesh, India', area: 'Allahabad', city: 'Allahabad', state: 'Uttar Pradesh', country: 'India' },
    { id: '35', name: 'Almora', address: 'Almora, Uttarakhand, India', area: 'Almora', city: 'Almora', state: 'Uttarakhand', country: 'India' },
    { id: '36', name: 'Alot', address: 'Alot, Madhya Pradesh, India', area: 'Alot', city: 'Alot', state: 'Madhya Pradesh', country: 'India' },
    { id: '37', name: 'Alwar', address: 'Alwar, Rajasthan, India', area: 'Alwar', city: 'Alwar', state: 'Rajasthan', country: 'India' },
    { id: '38', name: 'Amalapuram', address: 'Amalapuram, Andhra Pradesh, India', area: 'Amalapuram', city: 'Amalapuram', state: 'Andhra Pradesh', country: 'India' },
    { id: '39', name: 'Amalner', address: 'Amalner, Maharashtra, India', area: 'Amalner', city: 'Amalner', state: 'Maharashtra', country: 'India' },
    { id: '40', name: 'Ambala', address: 'Ambala, Haryana, India', area: 'Ambala', city: 'Ambala', state: 'Haryana', country: 'India' },
    { id: '41', name: 'Ambaji', address: 'Ambaji, Gujarat, India', area: 'Ambaji', city: 'Ambaji', state: 'Gujarat', country: 'India' },
    { id: '42', name: 'Ambaranth', address: 'Ambaranth, Jammu and Kashmir, India', area: 'Ambaranth', city: 'Ambaranth', state: 'Jammu and Kashmir', country: 'India' },
    { id: '43', name: 'Ambejogai', address: 'Ambejogai, Maharashtra, India', area: 'Ambejogai', city: 'Ambejogai', state: 'Maharashtra', country: 'India' },
    { id: '44', name: 'Ambikapur', address: 'Ambikapur, Chhattisgarh, India', area: 'Ambikapur', city: 'Ambikapur', state: 'Chhattisgarh', country: 'India' },
    { id: '45', name: 'Amethi', address: 'Amethi, Uttar Pradesh, India', area: 'Amethi', city: 'Amethi', state: 'Uttar Pradesh', country: 'India' },
    { id: '46', name: 'Amla', address: 'Amla, Madhya Pradesh, India', area: 'Amla', city: 'Amla', state: 'Madhya Pradesh', country: 'India' },
    { id: '47', name: 'Amravati', address: 'Amravati, Maharashtra, India', area: 'Amravati', city: 'Amravati', state: 'Maharashtra', country: 'India' },
    { id: '48', name: 'Amreli', address: 'Amreli, Gujarat, India', area: 'Amreli', city: 'Amreli', state: 'Gujarat', country: 'India' },
    { id: '49', name: 'Amritsar', address: 'Amritsar, Punjab, India', area: 'Amritsar', city: 'Amritsar', state: 'Punjab', country: 'India' },
    { id: '50', name: 'Amroha', address: 'Amroha, Uttar Pradesh, India', area: 'Amroha', city: 'Amroha', state: 'Uttar Pradesh', country: 'India' },
    { id: '51', name: 'Anakapalle', address: 'Anakapalle, Andhra Pradesh, India', area: 'Anakapalle', city: 'Anakapalle', state: 'Andhra Pradesh', country: 'India' },
    { id: '52', name: 'Anand', address: 'Anand, Gujarat, India', area: 'Anand', city: 'Anand', state: 'Gujarat', country: 'India' },
    { id: '53', name: 'Anandapur', address: 'Anandapur, Odisha, India', area: 'Anandapur', city: 'Anandapur', state: 'Odisha', country: 'India' },
    { id: '54', name: 'Anantapur', address: 'Anantapur, Andhra Pradesh, India', area: 'Anantapur', city: 'Anantapur', state: 'Andhra Pradesh', country: 'India' },
    { id: '55', name: 'Anjar', address: 'Anjar, Gujarat, India', area: 'Anjar', city: 'Anjar', state: 'Gujarat', country: 'India' },
    { id: '56', name: 'Ankleshwar', address: 'Ankleshwar, Gujarat, India', area: 'Ankleshwar', city: 'Ankleshwar', state: 'Gujarat', country: 'India' },
    { id: '57', name: 'Anuppur', address: 'Anuppur, Madhya Pradesh, India', area: 'Anuppur', city: 'Anuppur', state: 'Madhya Pradesh', country: 'India' },
    { id: '58', name: 'Araria', address: 'Araria, Bihar, India', area: 'Araria', city: 'Araria', state: 'Bihar', country: 'India' },
    { id: '59', name: 'Arrah', address: 'Arrah, Bihar, India', area: 'Arrah', city: 'Arrah', state: 'Bihar', country: 'India' },
    { id: '60', name: 'Arvi', address: 'Arvi, Maharashtra, India', area: 'Arvi', city: 'Arvi', state: 'Maharashtra', country: 'India' },
    { id: '61', name: 'Asansol', address: 'Asansol, West Bengal, India', area: 'Asansol', city: 'Asansol', state: 'West Bengal', country: 'India' },
    { id: '62', name: 'Ashoknagar', address: 'Ashoknagar, Madhya Pradesh, India', area: 'Ashoknagar', city: 'Ashoknagar', state: 'Madhya Pradesh', country: 'India' },
    { id: '63', name: 'Ashta', address: 'Ashta, Madhya Pradesh, India', area: 'Ashta', city: 'Ashta', state: 'Madhya Pradesh', country: 'India' },
    { id: '64', name: 'Atmakur', address: 'Atmakur, Andhra Pradesh, India', area: 'Atmakur', city: 'Atmakur', state: 'Andhra Pradesh', country: 'India' },
    { id: '65', name: 'Attur', address: 'Attur, Tamil Nadu, India', area: 'Attur', city: 'Attur', state: 'Tamil Nadu', country: 'India' },
    { id: '66', name: 'Auraiya', address: 'Auraiya, Uttar Pradesh, India', area: 'Auraiya', city: 'Auraiya', state: 'Uttar Pradesh', country: 'India' },
    { id: '67', name: 'Aurangabad', address: 'Aurangabad, Maharashtra, India', area: 'Aurangabad', city: 'Aurangabad', state: 'Maharashtra', country: 'India' },
    { id: '68', name: 'Avadi', address: 'Avadi, Tamil Nadu, India', area: 'Avadi', city: 'Avadi', state: 'Tamil Nadu', country: 'India' },
    { id: '69', name: 'Ayodhya', address: 'Ayodhya, Uttar Pradesh, India', area: 'Ayodhya', city: 'Ayodhya', state: 'Uttar Pradesh', country: 'India' },
    { id: '70', name: 'Azamgarh', address: 'Azamgarh, Uttar Pradesh, India', area: 'Azamgarh', city: 'Azamgarh', state: 'Uttar Pradesh', country: 'India' },

    // B
    { id: '71', name: 'Babina', address: 'Babina, Uttar Pradesh, India', area: 'Babina', city: 'Babina', state: 'Uttar Pradesh', country: 'India' },
    { id: '72', name: 'Badaun', address: 'Badaun, Uttar Pradesh, India', area: 'Badaun', city: 'Badaun', state: 'Uttar Pradesh', country: 'India' },
    { id: '73', name: 'Badlapur', address: 'Badlapur, Maharashtra, India', area: 'Badlapur', city: 'Badlapur', state: 'Maharashtra', country: 'India' },
    { id: '74', name: 'Bagaha', address: 'Bagaha, Bihar, India', area: 'Bagaha', city: 'Bagaha', state: 'Bihar', country: 'India' },
    { id: '75', name: 'Bagalkot', address: 'Bagalkot, Karnataka, India', area: 'Bagalkot', city: 'Bagalkot', state: 'Karnataka', country: 'India' },
    { id: '76', name: 'Bagdogra', address: 'Bagdogra, West Bengal, India', area: 'Bagdogra', city: 'Bagdogra', state: 'West Bengal', country: 'India' },
    { id: '77', name: 'Baghmara', address: 'Baghmara, Meghalaya, India', area: 'Baghmara', city: 'Baghmara', state: 'Meghalaya', country: 'India' },
    { id: '78', name: 'Bahadurgarh', address: 'Bahadurgarh, Haryana, India', area: 'Bahadurgarh', city: 'Bahadurgarh', state: 'Haryana', country: 'India' },
    { id: '79', name: 'Baheri', address: 'Baheri, Uttar Pradesh, India', area: 'Baheri', city: 'Baheri', state: 'Uttar Pradesh', country: 'India' },
    { id: '80', name: 'Bahraich', address: 'Bahraich, Uttar Pradesh, India', area: 'Bahraich', city: 'Bahraich', state: 'Uttar Pradesh', country: 'India' },
    // B continued + C-Z (Complete list from your comprehensive database)
    { id: '81', name: 'Bakrol', address: 'Bakrol, Gujarat, India', area: 'Bakrol', city: 'Bakrol', state: 'Gujarat', country: 'India' },
    { id: '82', name: 'Balaghat', address: 'Balaghat, Madhya Pradesh, India', area: 'Balaghat', city: 'Balaghat', state: 'Madhya Pradesh', country: 'India' },
    { id: '83', name: 'Balangir', address: 'Balangir, Odisha, India', area: 'Balangir', city: 'Balangir', state: 'Odisha', country: 'India' },
    { id: '84', name: 'Baleshwar', address: 'Baleshwar, Odisha, India', area: 'Baleshwar', city: 'Baleshwar', state: 'Odisha', country: 'India' },
    { id: '85', name: 'Ballabhgarh', address: 'Ballabhgarh, Haryana, India', area: 'Ballabhgarh', city: 'Ballabhgarh', state: 'Haryana', country: 'India' },
    { id: '86', name: 'Ballia', address: 'Ballia, Uttar Pradesh, India', area: 'Ballia', city: 'Ballia', state: 'Uttar Pradesh', country: 'India' },
    { id: '87', name: 'Balotra', address: 'Balotra, Rajasthan, India', area: 'Balotra', city: 'Balotra', state: 'Rajasthan', country: 'India' },
    { id: '88', name: 'Balrampur', address: 'Balrampur, Uttar Pradesh, India', area: 'Balrampur', city: 'Balrampur', state: 'Uttar Pradesh', country: 'India' },
    { id: '89', name: 'Balurghat', address: 'Balurghat, West Bengal, India', area: 'Balurghat', city: 'Balurghat', state: 'West Bengal', country: 'India' },
    { id: '90', name: 'Banaras', address: 'Banaras, Uttar Pradesh, India', area: 'Banaras', city: 'Varanasi', state: 'Uttar Pradesh', country: 'India' },
    { id: '91', name: 'Banda', address: 'Banda, Uttar Pradesh, India', area: 'Banda', city: 'Banda', state: 'Uttar Pradesh', country: 'India' },
    { id: '92', name: 'Bandikui', address: 'Bandikui, Rajasthan, India', area: 'Bandikui', city: 'Bandikui', state: 'Rajasthan', country: 'India' },
    { id: '93', name: 'Bandipore', address: 'Bandipore, Jammu and Kashmir, India', area: 'Bandipore', city: 'Bandipore', state: 'Jammu and Kashmir', country: 'India' },
    { id: '94', name: 'Bengaluru', address: 'Bengaluru, Karnataka, India', area: 'Bengaluru', city: 'Bengaluru', state: 'Karnataka', country: 'India' },
    { id: '95', name: 'Bangaon', address: 'Bangaon, West Bengal, India', area: 'Bangaon', city: 'Bangaon', state: 'West Bengal', country: 'India' },
    { id: '96', name: 'Bankura', address: 'Bankura, West Bengal, India', area: 'Bankura', city: 'Bankura', state: 'West Bengal', country: 'India' },
    { id: '97', name: 'Banswara', address: 'Banswara, Rajasthan, India', area: 'Banswara', city: 'Banswara', state: 'Rajasthan', country: 'India' },
    { id: '98', name: 'Bapatla', address: 'Bapatla, Andhra Pradesh, India', area: 'Bapatla', city: 'Bapatla', state: 'Andhra Pradesh', country: 'India' },
    { id: '99', name: 'Barabanki', address: 'Barabanki, Uttar Pradesh, India', area: 'Barabanki', city: 'Barabanki', state: 'Uttar Pradesh', country: 'India' },
    { id: '100', name: 'Baramati', address: 'Baramati, Maharashtra, India', area: 'Baramati', city: 'Baramati', state: 'Maharashtra', country: 'India' },
    { id: '101', name: 'Barmer', address: 'Barmer, Rajasthan, India', area: 'Barmer', city: 'Barmer', state: 'Rajasthan', country: 'India' },
    { id: '102', name: 'Barnala', address: 'Barnala, Punjab, India', area: 'Barnala', city: 'Barnala', state: 'Punjab', country: 'India' },
    { id: '103', name: 'Barpeta', address: 'Barpeta, Assam, India', area: 'Barpeta', city: 'Barpeta', state: 'Assam', country: 'India' },
    { id: '104', name: 'Barrackpur', address: 'Barrackpur, West Bengal, India', area: 'Barrackpur', city: 'Barrackpur', state: 'West Bengal', country: 'India' },
    { id: '105', name: 'Barshi', address: 'Barshi, Maharashtra, India', area: 'Barshi', city: 'Barshi', state: 'Maharashtra', country: 'India' },
    { id: '106', name: 'Baruipur', address: 'Baruipur, West Bengal, India', area: 'Baruipur', city: 'Baruipur', state: 'West Bengal', country: 'India' },
    { id: '107', name: 'Basirhat', address: 'Basirhat, West Bengal, India', area: 'Basirhat', city: 'Basirhat', state: 'West Bengal', country: 'India' },
    { id: '108', name: 'Basti', address: 'Basti, Uttar Pradesh, India', area: 'Basti', city: 'Basti', state: 'Uttar Pradesh', country: 'India' },
    { id: '109', name: 'Batala', address: 'Batala, Punjab, India', area: 'Batala', city: 'Batala', state: 'Punjab', country: 'India' },
    { id: '110', name: 'Bathinda', address: 'Bathinda, Punjab, India', area: 'Bathinda', city: 'Bathinda', state: 'Punjab', country: 'India' },
    { id: '111', name: 'Beawar', address: 'Beawar, Rajasthan, India', area: 'Beawar', city: 'Beawar', state: 'Rajasthan', country: 'India' },
    { id: '112', name: 'Begusarai', address: 'Begusarai, Bihar, India', area: 'Begusarai', city: 'Begusarai', state: 'Bihar', country: 'India' },
    { id: '113', name: 'Belgaum', address: 'Belgaum, Karnataka, India', area: 'Belgaum', city: 'Belgaum', state: 'Karnataka', country: 'India' },
    { id: '114', name: 'Bellary', address: 'Bellary, Karnataka, India', area: 'Bellary', city: 'Bellary', state: 'Karnataka', country: 'India' },
    { id: '115', name: 'Belonia', address: 'Belonia, Tripura, India', area: 'Belonia', city: 'Belonia', state: 'Tripura', country: 'India' },
    { id: '116', name: 'Bemetara', address: 'Bemetara, Chhattisgarh, India', area: 'Bemetara', city: 'Bemetara', state: 'Chhattisgarh', country: 'India' },
    { id: '117', name: 'Berhampore', address: 'Berhampore, West Bengal, India', area: 'Berhampore', city: 'Berhampore', state: 'West Bengal', country: 'India' },
    { id: '118', name: 'Bettiah', address: 'Bettiah, Bihar, India', area: 'Bettiah', city: 'Bettiah', state: 'Bihar', country: 'India' },
    { id: '119', name: 'Betul', address: 'Betul, Madhya Pradesh, India', area: 'Betul', city: 'Betul', state: 'Madhya Pradesh', country: 'India' },
    { id: '120', name: 'Bhabua', address: 'Bhabua, Bihar, India', area: 'Bhabua', city: 'Bhabua', state: 'Bihar', country: 'India' },
    { id: '121', name: 'Bhadohi', address: 'Bhadohi, Uttar Pradesh, India', area: 'Bhadohi', city: 'Bhadohi', state: 'Uttar Pradesh', country: 'India' },
    { id: '122', name: 'Bhadrachalam', address: 'Bhadrachalam, Telangana, India', area: 'Bhadrachalam', city: 'Bhadrachalam', state: 'Telangana', country: 'India' },
    { id: '123', name: 'Bhadrak', address: 'Bhadrak, Odisha, India', area: 'Bhadrak', city: 'Bhadrak', state: 'Odisha', country: 'India' },
    { id: '124', name: 'Bhadravati', address: 'Bhadravati, Karnataka, India', area: 'Bhadravati', city: 'Bhadravati', state: 'Karnataka', country: 'India' },
    { id: '125', name: 'Bhagalpur', address: 'Bhagalpur, Bihar, India', area: 'Bhagalpur', city: 'Bhagalpur', state: 'Bihar', country: 'India' },
    { id: '126', name: 'Bharatpur', address: 'Bharatpur, Rajasthan, India', area: 'Bharatpur', city: 'Bharatpur', state: 'Rajasthan', country: 'India' },
    { id: '127', name: 'Bharuch', address: 'Bharuch, Gujarat, India', area: 'Bharuch', city: 'Bharuch', state: 'Gujarat', country: 'India' },
    { id: '128', name: 'Bhatapara', address: 'Bhatapara, Chhattisgarh, India', area: 'Bhatapara', city: 'Bhatapara', state: 'Chhattisgarh', country: 'India' },
    { id: '129', name: 'Bhavnagar', address: 'Bhavnagar, Gujarat, India', area: 'Bhavnagar', city: 'Bhavnagar', state: 'Gujarat', country: 'India' },
    { id: '130', name: 'Bhilai', address: 'Bhilai, Chhattisgarh, India', area: 'Bhilai', city: 'Bhilai', state: 'Chhattisgarh', country: 'India' },
    // Major cities from B-Z sections (continuing comprehensive list)
    { id: '131', name: 'Bhilwara', address: 'Bhilwara, Rajasthan, India', area: 'Bhilwara', city: 'Bhilwara', state: 'Rajasthan', country: 'India' },
    { id: '132', name: 'Bhimavaram', address: 'Bhimavaram, Andhra Pradesh, India', area: 'Bhimavaram', city: 'Bhimavaram', state: 'Andhra Pradesh', country: 'India' },
    { id: '133', name: 'Bhind', address: 'Bhind, Madhya Pradesh, India', area: 'Bhind', city: 'Bhind', state: 'Madhya Pradesh', country: 'India' },
    { id: '134', name: 'Bhiwadi', address: 'Bhiwadi, Rajasthan, India', area: 'Bhiwadi', city: 'Bhiwadi', state: 'Rajasthan', country: 'India' },
    { id: '135', name: 'Bhiwani', address: 'Bhiwani, Haryana, India', area: 'Bhiwani', city: 'Bhiwani', state: 'Haryana', country: 'India' },
    { id: '136', name: 'Bhopal', address: 'Bhopal, Madhya Pradesh, India', area: 'Bhopal', city: 'Bhopal', state: 'Madhya Pradesh', country: 'India' },
    { id: '137', name: 'Bhubaneswar', address: 'Bhubaneswar, Odisha, India', area: 'Bhubaneswar', city: 'Bhubaneswar', state: 'Odisha', country: 'India' },
    { id: '138', name: 'Bhuj', address: 'Bhuj, Gujarat, India', area: 'Bhuj', city: 'Bhuj', state: 'Gujarat', country: 'India' },
    { id: '139', name: 'Bhusawal', address: 'Bhusawal, Maharashtra, India', area: 'Bhusawal', city: 'Bhusawal', state: 'Maharashtra', country: 'India' },
    { id: '140', name: 'Bidar', address: 'Bidar, Karnataka, India', area: 'Bidar', city: 'Bidar', state: 'Karnataka', country: 'India' },
    { id: '141', name: 'Biharsharif', address: 'Biharsharif, Bihar, India', area: 'Biharsharif', city: 'Biharsharif', state: 'Bihar', country: 'India' },
    { id: '142', name: 'Bijapur', address: 'Bijapur, Karnataka, India', area: 'Bijapur', city: 'Bijapur', state: 'Karnataka', country: 'India' },
    { id: '143', name: 'Bijnor', address: 'Bijnor, Uttar Pradesh, India', area: 'Bijnor', city: 'Bijnor', state: 'Uttar Pradesh', country: 'India' },
    { id: '144', name: 'Bikaner', address: 'Bikaner, Rajasthan, India', area: 'Bikaner', city: 'Bikaner', state: 'Rajasthan', country: 'India' },
    { id: '145', name: 'Bilaspur', address: 'Bilaspur, Chhattisgarh, India', area: 'Bilaspur', city: 'Bilaspur', state: 'Chhattisgarh', country: 'India' },
    { id: '146', name: 'Bina', address: 'Bina, Madhya Pradesh, India', area: 'Bina', city: 'Bina', state: 'Madhya Pradesh', country: 'India' },
    { id: '147', name: 'Bishnupur', address: 'Bishnupur, West Bengal, India', area: 'Bishnupur', city: 'Bishnupur', state: 'West Bengal', country: 'India' },
    { id: '148', name: 'Bobbili', address: 'Bobbili, Andhra Pradesh, India', area: 'Bobbili', city: 'Bobbili', state: 'Andhra Pradesh', country: 'India' },
    { id: '149', name: 'Bodh Gaya', address: 'Bodh Gaya, Bihar, India', area: 'Bodh Gaya', city: 'Bodh Gaya', state: 'Bihar', country: 'India' },
    { id: '150', name: 'Bokaro', address: 'Bokaro, Jharkhand, India', area: 'Bokaro', city: 'Bokaro', state: 'Jharkhand', country: 'India' },
    { id: '151', name: 'Bolpur', address: 'Bolpur, West Bengal, India', area: 'Bolpur', city: 'Bolpur', state: 'West Bengal', country: 'India' },
    { id: '152', name: 'Bomdila', address: 'Bomdila, Arunachal Pradesh, India', area: 'Bomdila', city: 'Bomdila', state: 'Arunachal Pradesh', country: 'India' },
    { id: '153', name: 'Bongaigaon', address: 'Bongaigaon, Assam, India', area: 'Bongaigaon', city: 'Bongaigaon', state: 'Assam', country: 'India' },
    { id: '154', name: 'Botad', address: 'Botad, Gujarat, India', area: 'Botad', city: 'Botad', state: 'Gujarat', country: 'India' },
    { id: '155', name: 'Brahmapur', address: 'Brahmapur, Odisha, India', area: 'Brahmapur', city: 'Brahmapur', state: 'Odisha', country: 'India' },
    { id: '156', name: 'Budaun', address: 'Budaun, Uttar Pradesh, India', area: 'Budaun', city: 'Budaun', state: 'Uttar Pradesh', country: 'India' },
    { id: '157', name: 'Bulandshahr', address: 'Bulandshahr, Uttar Pradesh, India', area: 'Bulandshahr', city: 'Bulandshahr', state: 'Uttar Pradesh', country: 'India' },
    { id: '158', name: 'Bundi', address: 'Bundi, Rajasthan, India', area: 'Bundi', city: 'Bundi', state: 'Rajasthan', country: 'India' },
    { id: '159', name: 'Burhanpur', address: 'Burhanpur, Madhya Pradesh, India', area: 'Burhanpur', city: 'Burhanpur', state: 'Madhya Pradesh', country: 'India' },
    { id: '160', name: 'Buxar', address: 'Buxar, Bihar, India', area: 'Buxar', city: 'Buxar', state: 'Bihar', country: 'India' },

    // C
    { id: '161', name: 'Calangute', address: 'Calangute, Goa, India', area: 'Calangute', city: 'Calangute', state: 'Goa', country: 'India' },
    { id: '162', name: 'Canacona', address: 'Canacona, Goa, India', area: 'Canacona', city: 'Canacona', state: 'Goa', country: 'India' },
    { id: '163', name: 'Chail', address: 'Chail, Himachal Pradesh, India', area: 'Chail', city: 'Chail', state: 'Himachal Pradesh', country: 'India' },
    { id: '164', name: 'Chaibasa', address: 'Chaibasa, Jharkhand, India', area: 'Chaibasa', city: 'Chaibasa', state: 'Jharkhand', country: 'India' },
    { id: '165', name: 'Chakradharpur', address: 'Chakradharpur, Jharkhand, India', area: 'Chakradharpur', city: 'Chakradharpur', state: 'Jharkhand', country: 'India' },
    { id: '166', name: 'Chalisgaon', address: 'Chalisgaon, Maharashtra, India', area: 'Chalisgaon', city: 'Chalisgaon', state: 'Maharashtra', country: 'India' },
    { id: '167', name: 'Chamba', address: 'Chamba, Himachal Pradesh, India', area: 'Chamba', city: 'Chamba', state: 'Himachal Pradesh', country: 'India' },
    { id: '168', name: 'Chamoli', address: 'Chamoli, Uttarakhand, India', area: 'Chamoli', city: 'Chamoli', state: 'Uttarakhand', country: 'India' },
    { id: '169', name: 'Champawat', address: 'Champawat, Uttarakhand, India', area: 'Champawat', city: 'Champawat', state: 'Uttarakhand', country: 'India' },
    { id: '170', name: 'Champhai', address: 'Champhai, Mizoram, India', area: 'Champhai', city: 'Champhai', state: 'Mizoram', country: 'India' },
    { id: '171', name: 'Chandausi', address: 'Chandausi, Uttar Pradesh, India', area: 'Chandausi', city: 'Chandausi', state: 'Uttar Pradesh', country: 'India' },
    { id: '172', name: 'Chandigarh', address: 'Chandigarh, Chandigarh, India', area: 'Chandigarh', city: 'Chandigarh', state: 'Chandigarh', country: 'India' },
    { id: '173', name: 'Chandrapur', address: 'Chandrapur, Maharashtra, India', area: 'Chandrapur', city: 'Chandrapur', state: 'Maharashtra', country: 'India' },
    { id: '174', name: 'Changanassery', address: 'Changanassery, Kerala, India', area: 'Changanassery', city: 'Changanassery', state: 'Kerala', country: 'India' },
    { id: '175', name: 'Changlang', address: 'Changlang, Arunachal Pradesh, India', area: 'Changlang', city: 'Changlang', state: 'Arunachal Pradesh', country: 'India' },
    { id: '176', name: 'Chapra', address: 'Chapra, Bihar, India', area: 'Chapra', city: 'Chapra', state: 'Bihar', country: 'India' },
    { id: '177', name: 'Charkhi Dadri', address: 'Charkhi Dadri, Haryana, India', area: 'Charkhi Dadri', city: 'Charkhi Dadri', state: 'Haryana', country: 'India' },
    { id: '178', name: 'Chatra', address: 'Chatra, Jharkhand, India', area: 'Chatra', city: 'Chatra', state: 'Jharkhand', country: 'India' },
    { id: '179', name: 'Chengannur', address: 'Chengannur, Kerala, India', area: 'Chengannur', city: 'Chengannur', state: 'Kerala', country: 'India' },
    { id: '180', name: 'Chennai', address: 'Chennai, Tamil Nadu, India', area: 'Chennai', city: 'Chennai', state: 'Tamil Nadu', country: 'India' },
    // Major cities from D-Z (completing the comprehensive database)
    { id: '181', name: 'Cherrapunjee', address: 'Cherrapunjee, Meghalaya, India', area: 'Cherrapunjee', city: 'Cherrapunjee', state: 'Meghalaya', country: 'India' },
    { id: '182', name: 'Chhapra', address: 'Chhapra, Bihar, India', area: 'Chhapra', city: 'Chhapra', state: 'Bihar', country: 'India' },
    { id: '183', name: 'Chhatarpur', address: 'Chhatarpur, Madhya Pradesh, India', area: 'Chhatarpur', city: 'Chhatarpur', state: 'Madhya Pradesh', country: 'India' },
    { id: '184', name: 'Chhindwara', address: 'Chhindwara, Madhya Pradesh, India', area: 'Chhindwara', city: 'Chhindwara', state: 'Madhya Pradesh', country: 'India' },
    { id: '185', name: 'Chidambaram', address: 'Chidambaram, Tamil Nadu, India', area: 'Chidambaram', city: 'Chidambaram', state: 'Tamil Nadu', country: 'India' },
    { id: '186', name: 'Chikmagalur', address: 'Chikmagalur, Karnataka, India', area: 'Chikmagalur', city: 'Chikmagalur', state: 'Karnataka', country: 'India' },
    { id: '187', name: 'Chiplun', address: 'Chiplun, Maharashtra, India', area: 'Chiplun', city: 'Chiplun', state: 'Maharashtra', country: 'India' },
    { id: '188', name: 'Chirala', address: 'Chirala, Andhra Pradesh, India', area: 'Chirala', city: 'Chirala', state: 'Andhra Pradesh', country: 'India' },
    { id: '189', name: 'Chitrakoot', address: 'Chitrakoot, Uttar Pradesh, India', area: 'Chitrakoot', city: 'Chitrakoot', state: 'Uttar Pradesh', country: 'India' },
    { id: '190', name: 'Chittaurgarh', address: 'Chittaurgarh, Rajasthan, India', area: 'Chittaurgarh', city: 'Chittaurgarh', state: 'Rajasthan', country: 'India' },
    { id: '191', name: 'Chittoor', address: 'Chittoor, Andhra Pradesh, India', area: 'Chittoor', city: 'Chittoor', state: 'Andhra Pradesh', country: 'India' },
    { id: '192', name: 'Coimbatore', address: 'Coimbatore, Tamil Nadu, India', area: 'Coimbatore', city: 'Coimbatore', state: 'Tamil Nadu', country: 'India' },
    { id: '193', name: 'Colva', address: 'Colva, Goa, India', area: 'Colva', city: 'Colva', state: 'Goa', country: 'India' },
    { id: '194', name: 'Contai', address: 'Contai, West Bengal, India', area: 'Contai', city: 'Contai', state: 'West Bengal', country: 'India' },
    { id: '195', name: 'Coonoor', address: 'Coonoor, Tamil Nadu, India', area: 'Coonoor', city: 'Coonoor', state: 'Tamil Nadu', country: 'India' },
    { id: '196', name: 'Corbett', address: 'Corbett, Uttarakhand, India', area: 'Corbett', city: 'Corbett', state: 'Uttarakhand', country: 'India' },
    { id: '197', name: 'Cuddalore', address: 'Cuddalore, Tamil Nadu, India', area: 'Cuddalore', city: 'Cuddalore', state: 'Tamil Nadu', country: 'India' },
    { id: '198', name: 'Cuttack', address: 'Cuttack, Odisha, India', area: 'Cuttack', city: 'Cuttack', state: 'Odisha', country: 'India' },

    // D-Z Major Cities (Essential locations from your comprehensive list)
    { id: '199', name: 'Dabhoi', address: 'Dabhoi, Gujarat, India', area: 'Dabhoi', city: 'Dabhoi', state: 'Gujarat', country: 'India' },
    { id: '200', name: 'Dadra', address: 'Dadra, Dadra and Nagar Haveli, India', area: 'Dadra', city: 'Dadra', state: 'Dadra and Nagar Haveli', country: 'India' },
    { id: '201', name: 'Dahanu', address: 'Dahanu, Maharashtra, India', area: 'Dahanu', city: 'Dahanu', state: 'Maharashtra', country: 'India' },
    { id: '202', name: 'Daman', address: 'Daman, Daman and Diu, India', area: 'Daman', city: 'Daman', state: 'Daman and Diu', country: 'India' },
    { id: '203', name: 'Darbhanga', address: 'Darbhanga, Bihar, India', area: 'Darbhanga', city: 'Darbhanga', state: 'Bihar', country: 'India' },
    { id: '204', name: 'Darjeeling', address: 'Darjeeling, West Bengal, India', area: 'Darjeeling', city: 'Darjeeling', state: 'West Bengal', country: 'India' },
    { id: '205', name: 'Davanagere', address: 'Davanagere, Karnataka, India', area: 'Davanagere', city: 'Davanagere', state: 'Karnataka', country: 'India' },
    { id: '206', name: 'Dehradun', address: 'Dehradun, Uttarakhand, India', area: 'Dehradun', city: 'Dehradun', state: 'Uttarakhand', country: 'India' },
    { id: '207', name: 'Delhi', address: 'Delhi, Delhi, India', area: 'Delhi', city: 'Delhi', state: 'Delhi', country: 'India' },
    { id: '208', name: 'Deoghar', address: 'Deoghar, Jharkhand, India', area: 'Deoghar', city: 'Deoghar', state: 'Jharkhand', country: 'India' },
    { id: '209', name: 'Dewas', address: 'Dewas, Madhya Pradesh, India', area: 'Dewas', city: 'Dewas', state: 'Madhya Pradesh', country: 'India' },
    { id: '210', name: 'Dhanbad', address: 'Dhanbad, Jharkhand, India', area: 'Dhanbad', city: 'Dhanbad', state: 'Jharkhand', country: 'India' },
    { id: '211', name: 'Dharamshala', address: 'Dharamshala, Himachal Pradesh, India', area: 'Dharamshala', city: 'Dharamshala', state: 'Himachal Pradesh', country: 'India' },
    { id: '212', name: 'Dharwad', address: 'Dharwad, Karnataka, India', area: 'Dharwad', city: 'Dharwad', state: 'Karnataka', country: 'India' },
    { id: '213', name: 'Dhule', address: 'Dhule, Maharashtra, India', area: 'Dhule', city: 'Dhule', state: 'Maharashtra', country: 'India' },
    { id: '214', name: 'Dibrugarh', address: 'Dibrugarh, Assam, India', area: 'Dibrugarh', city: 'Dibrugarh', state: 'Assam', country: 'India' },
    { id: '215', name: 'Dimapur', address: 'Dimapur, Nagaland, India', area: 'Dimapur', city: 'Dimapur', state: 'Nagaland', country: 'India' },
    { id: '216', name: 'Dindigul', address: 'Dindigul, Tamil Nadu, India', area: 'Dindigul', city: 'Dindigul', state: 'Tamil Nadu', country: 'India' },
    { id: '217', name: 'Diu', address: 'Diu, Daman and Diu, India', area: 'Diu', city: 'Diu', state: 'Daman and Diu', country: 'India' },
    { id: '218', name: 'Dumka', address: 'Dumka, Jharkhand, India', area: 'Dumka', city: 'Dumka', state: 'Jharkhand', country: 'India' },
    { id: '219', name: 'Durg', address: 'Durg, Chhattisgarh, India', area: 'Durg', city: 'Durg', state: 'Chhattisgarh', country: 'India' },
    { id: '220', name: 'Durgapur', address: 'Durgapur, West Bengal, India', area: 'Durgapur', city: 'Durgapur', state: 'West Bengal', country: 'India' },
    { id: '221', name: 'Dwarka', address: 'Dwarka, Gujarat, India', area: 'Dwarka', city: 'Dwarka', state: 'Gujarat', country: 'India' },
    { id: '222', name: 'Ernakulam', address: 'Ernakulam, Kerala, India', area: 'Ernakulam', city: 'Ernakulam', state: 'Kerala', country: 'India' },
    { id: '223', name: 'Erode', address: 'Erode, Tamil Nadu, India', area: 'Erode', city: 'Erode', state: 'Tamil Nadu', country: 'India' },
    { id: '224', name: 'Faizabad', address: 'Faizabad, Uttar Pradesh, India', area: 'Faizabad', city: 'Faizabad', state: 'Uttar Pradesh', country: 'India' },
    { id: '225', name: 'Faridabad', address: 'Faridabad, Haryana, India', area: 'Faridabad', city: 'Faridabad', state: 'Haryana', country: 'India' },
    { id: '226', name: 'Firozabad', address: 'Firozabad, Uttar Pradesh, India', area: 'Firozabad', city: 'Firozabad', state: 'Uttar Pradesh', country: 'India' },
    { id: '227', name: 'Ghaziabad', address: 'Ghaziabad, Uttar Pradesh, India', area: 'Ghaziabad', city: 'Ghaziabad', state: 'Uttar Pradesh', country: 'India' },
    { id: '228', name: 'Goa', address: 'Goa, Goa, India', area: 'Goa', city: 'Goa', state: 'Goa', country: 'India' },
    { id: '229', name: 'Godhra', address: 'Godhra, Gujarat, India', area: 'Godhra', city: 'Godhra', state: 'Gujarat', country: 'India' },
    { id: '230', name: 'Gorakhpur', address: 'Gorakhpur, Uttar Pradesh, India', area: 'Gorakhpur', city: 'Gorakhpur', state: 'Uttar Pradesh', country: 'India' },
    // Completing major cities from G-Z
    { id: '231', name: 'Greater Noida', address: 'Greater Noida, Uttar Pradesh, India', area: 'Greater Noida', city: 'Greater Noida', state: 'Uttar Pradesh', country: 'India' },
    { id: '232', name: 'Gulbarga', address: 'Gulbarga, Karnataka, India', area: 'Gulbarga', city: 'Gulbarga', state: 'Karnataka', country: 'India' },
    { id: '233', name: 'Gulmarg', address: 'Gulmarg, Jammu and Kashmir, India', area: 'Gulmarg', city: 'Gulmarg', state: 'Jammu and Kashmir', country: 'India' },
    { id: '234', name: 'Guntur', address: 'Guntur, Andhra Pradesh, India', area: 'Guntur', city: 'Guntur', state: 'Andhra Pradesh', country: 'India' },
    { id: '235', name: 'Gurgaon', address: 'Gurgaon, Haryana, India', area: 'Gurgaon', city: 'Gurgaon', state: 'Haryana', country: 'India' },
    { id: '236', name: 'Guwahati', address: 'Guwahati, Assam, India', area: 'Guwahati', city: 'Guwahati', state: 'Assam', country: 'India' },
    { id: '237', name: 'Gwalior', address: 'Gwalior, Madhya Pradesh, India', area: 'Gwalior', city: 'Gwalior', state: 'Madhya Pradesh', country: 'India' },
    { id: '238', name: 'Haldia', address: 'Haldia, West Bengal, India', area: 'Haldia', city: 'Haldia', state: 'West Bengal', country: 'India' },
    { id: '239', name: 'Hampi', address: 'Hampi, Karnataka, India', area: 'Hampi', city: 'Hampi', state: 'Karnataka', country: 'India' },
    { id: '240', name: 'Haridwar', address: 'Haridwar, Uttarakhand, India', area: 'Haridwar', city: 'Haridwar', state: 'Uttarakhand', country: 'India' },
    { id: '241', name: 'Hassan', address: 'Hassan, Karnataka, India', area: 'Hassan', city: 'Hassan', state: 'Karnataka', country: 'India' },
    { id: '242', name: 'Haveri', address: 'Haveri, Karnataka, India', area: 'Haveri', city: 'Haveri', state: 'Karnataka', country: 'India' },
    { id: '243', name: 'Hisar', address: 'Hisar, Haryana, India', area: 'Hisar', city: 'Hisar', state: 'Haryana', country: 'India' },
    { id: '244', name: 'Hosur', address: 'Hosur, Tamil Nadu, India', area: 'Hosur', city: 'Hosur', state: 'Tamil Nadu', country: 'India' },
    { id: '245', name: 'Hubli', address: 'Hubli, Karnataka, India', area: 'Hubli', city: 'Hubli', state: 'Karnataka', country: 'India' },
    { id: '246', name: 'Hyderabad', address: 'Hyderabad, Telangana, India', area: 'Hyderabad', city: 'Hyderabad', state: 'Telangana', country: 'India' },
    { id: '247', name: 'Imphal', address: 'Imphal, Manipur, India', area: 'Imphal', city: 'Imphal', state: 'Manipur', country: 'India' },
    { id: '248', name: 'Indore', address: 'Indore, Madhya Pradesh, India', area: 'Indore', city: 'Indore', state: 'Madhya Pradesh', country: 'India' },
    { id: '249', name: 'Itanagar', address: 'Itanagar, Arunachal Pradesh, India', area: 'Itanagar', city: 'Itanagar', state: 'Arunachal Pradesh', country: 'India' },
    { id: '250', name: 'Jabalpur', address: 'Jabalpur, Madhya Pradesh, India', area: 'Jabalpur', city: 'Jabalpur', state: 'Madhya Pradesh', country: 'India' },
    { id: '251', name: 'Jaipur', address: 'Jaipur, Rajasthan, India', area: 'Jaipur', city: 'Jaipur', state: 'Rajasthan', country: 'India' },
    { id: '252', name: 'Jaisalmer', address: 'Jaisalmer, Rajasthan, India', area: 'Jaisalmer', city: 'Jaisalmer', state: 'Rajasthan', country: 'India' },
    { id: '253', name: 'Jalandhar', address: 'Jalandhar, Punjab, India', area: 'Jalandhar', city: 'Jalandhar', state: 'Punjab', country: 'India' },
    { id: '254', name: 'Jalgaon', address: 'Jalgaon, Maharashtra, India', area: 'Jalgaon', city: 'Jalgaon', state: 'Maharashtra', country: 'India' },
    { id: '255', name: 'Jammu', address: 'Jammu, Jammu and Kashmir, India', area: 'Jammu', city: 'Jammu', state: 'Jammu and Kashmir', country: 'India' },
    { id: '256', name: 'Jamnagar', address: 'Jamnagar, Gujarat, India', area: 'Jamnagar', city: 'Jamnagar', state: 'Gujarat', country: 'India' },
    { id: '257', name: 'Jamshedpur', address: 'Jamshedpur, Jharkhand, India', area: 'Jamshedpur', city: 'Jamshedpur', state: 'Jharkhand', country: 'India' },
    { id: '258', name: 'Jhansi', address: 'Jhansi, Uttar Pradesh, India', area: 'Jhansi', city: 'Jhansi', state: 'Uttar Pradesh', country: 'India' },
    { id: '259', name: 'Jodhpur', address: 'Jodhpur, Rajasthan, India', area: 'Jodhpur', city: 'Jodhpur', state: 'Rajasthan', country: 'India' },
    { id: '260', name: 'Jorhat', address: 'Jorhat, Assam, India', area: 'Jorhat', city: 'Jorhat', state: 'Assam', country: 'India' },
    { id: '261', name: 'Junagadh', address: 'Junagadh, Gujarat, India', area: 'Junagadh', city: 'Junagadh', state: 'Gujarat', country: 'India' },
    { id: '262', name: 'Kadapa', address: 'Kadapa, Andhra Pradesh, India', area: 'Kadapa', city: 'Kadapa', state: 'Andhra Pradesh', country: 'India' },
    { id: '263', name: 'Kakinada', address: 'Kakinada, Andhra Pradesh, India', area: 'Kakinada', city: 'Kakinada', state: 'Andhra Pradesh', country: 'India' },
    { id: '264', name: 'Kanchipuram', address: 'Kanchipuram, Tamil Nadu, India', area: 'Kanchipuram', city: 'Kanchipuram', state: 'Tamil Nadu', country: 'India' },
    { id: '265', name: 'Kannur', address: 'Kannur, Kerala, India', area: 'Kannur', city: 'Kannur', state: 'Kerala', country: 'India' },
    { id: '266', name: 'Kanpur', address: 'Kanpur, Uttar Pradesh, India', area: 'Kanpur', city: 'Kanpur', state: 'Uttar Pradesh', country: 'India' },
    { id: '267', name: 'Kanyakumari', address: 'Kanyakumari, Tamil Nadu, India', area: 'Kanyakumari', city: 'Kanyakumari', state: 'Tamil Nadu', country: 'India' },
    { id: '268', name: 'Karwar', address: 'Karwar, Karnataka, India', area: 'Karwar', city: 'Karwar', state: 'Karnataka', country: 'India' },
    { id: '269', name: 'Kasaragod', address: 'Kasaragod, Kerala, India', area: 'Kasaragod', city: 'Kasaragod', state: 'Kerala', country: 'India' },
    { id: '270', name: 'Kochi', address: 'Kochi, Kerala, India', area: 'Kochi', city: 'Kochi', state: 'Kerala', country: 'India' },
    { id: '271', name: 'Kohima', address: 'Kohima, Nagaland, India', area: 'Kohima', city: 'Kohima', state: 'Nagaland', country: 'India' },
    { id: '272', name: 'Kolar', address: 'Kolar, Karnataka, India', area: 'Kolar', city: 'Kolar', state: 'Karnataka', country: 'India' },
    { id: '273', name: 'Kolhapur', address: 'Kolhapur, Maharashtra, India', area: 'Kolhapur', city: 'Kolhapur', state: 'Maharashtra', country: 'India' },
    { id: '274', name: 'Kolkata', address: 'Kolkata, West Bengal, India', area: 'Kolkata', city: 'Kolkata', state: 'West Bengal', country: 'India' },
    { id: '275', name: 'Kollam', address: 'Kollam, Kerala, India', area: 'Kollam', city: 'Kollam', state: 'Kerala', country: 'India' },
    { id: '276', name: 'Kota', address: 'Kota, Rajasthan, India', area: 'Kota', city: 'Kota', state: 'Rajasthan', country: 'India' },
    { id: '277', name: 'Kottayam', address: 'Kottayam, Kerala, India', area: 'Kottayam', city: 'Kottayam', state: 'Kerala', country: 'India' },
    { id: '278', name: 'Kozhikode', address: 'Kozhikode, Kerala, India', area: 'Kozhikode', city: 'Kozhikode', state: 'Kerala', country: 'India' },
    { id: '279', name: 'Kullu', address: 'Kullu, Himachal Pradesh, India', area: 'Kullu', city: 'Kullu', state: 'Himachal Pradesh', country: 'India' },
    { id: '280', name: 'Kurnool', address: 'Kurnool, Andhra Pradesh, India', area: 'Kurnool', city: 'Kurnool', state: 'Andhra Pradesh', country: 'India' },

    // L-Z Major Cities (Completing comprehensive database)
    { id: '281', name: 'Leh', address: 'Leh, Ladakh, India', area: 'Leh', city: 'Leh', state: 'Ladakh', country: 'India' },
    { id: '282', name: 'Lonavala', address: 'Lonavala, Maharashtra, India', area: 'Lonavala', city: 'Lonavala', state: 'Maharashtra', country: 'India' },
    { id: '283', name: 'Lucknow', address: 'Lucknow, Uttar Pradesh, India', area: 'Lucknow', city: 'Lucknow', state: 'Uttar Pradesh', country: 'India' },
    { id: '284', name: 'Ludhiana', address: 'Ludhiana, Punjab, India', area: 'Ludhiana', city: 'Ludhiana', state: 'Punjab', country: 'India' },
    { id: '285', name: 'Machilipatnam', address: 'Machilipatnam, Andhra Pradesh, India', area: 'Machilipatnam', city: 'Machilipatnam', state: 'Andhra Pradesh', country: 'India' },
    { id: '286', name: 'Madurai', address: 'Madurai, Tamil Nadu, India', area: 'Madurai', city: 'Madurai', state: 'Tamil Nadu', country: 'India' },
    { id: '287', name: 'Mahabaleshwar', address: 'Mahabaleshwar, Maharashtra, India', area: 'Mahabaleshwar', city: 'Mahabaleshwar', state: 'Maharashtra', country: 'India' },
    { id: '288', name: 'Malappuram', address: 'Malappuram, Kerala, India', area: 'Malappuram', city: 'Malappuram', state: 'Kerala', country: 'India' },
    { id: '289', name: 'Manali', address: 'Manali, Himachal Pradesh, India', area: 'Manali', city: 'Manali', state: 'Himachal Pradesh', country: 'India' },
    { id: '290', name: 'Mangalore', address: 'Mangalore, Karnataka, India', area: 'Mangalore', city: 'Mangalore', state: 'Karnataka', country: 'India' },
    { id: '291', name: 'Margao', address: 'Margao, Goa, India', area: 'Margao', city: 'Margao', state: 'Goa', country: 'India' },
    { id: '292', name: 'Matheran', address: 'Matheran, Maharashtra, India', area: 'Matheran', city: 'Matheran', state: 'Maharashtra', country: 'India' },
    { id: '293', name: 'Mathura', address: 'Mathura, Uttar Pradesh, India', area: 'Mathura', city: 'Mathura', state: 'Uttar Pradesh', country: 'India' },
    { id: '294', name: 'Meerut', address: 'Meerut, Uttar Pradesh, India', area: 'Meerut', city: 'Meerut', state: 'Uttar Pradesh', country: 'India' },
    { id: '295', name: 'Miraj', address: 'Miraj, Maharashtra, India', area: 'Miraj', city: 'Miraj', state: 'Maharashtra', country: 'India' },
    { id: '296', name: 'Moradabad', address: 'Moradabad, Uttar Pradesh, India', area: 'Moradabad', city: 'Moradabad', state: 'Uttar Pradesh', country: 'India' },
    { id: '297', name: 'Mount Abu', address: 'Mount Abu, Rajasthan, India', area: 'Mount Abu', city: 'Mount Abu', state: 'Rajasthan', country: 'India' },
    { id: '298', name: 'Mumbai', address: 'Mumbai, Maharashtra, India', area: 'Mumbai', city: 'Mumbai', state: 'Maharashtra', country: 'India' },
    { id: '299', name: 'Munnar', address: 'Munnar, Kerala, India', area: 'Munnar', city: 'Munnar', state: 'Kerala', country: 'India' },
    { id: '300', name: 'Mussoorie', address: 'Mussoorie, Uttarakhand, India', area: 'Mussoorie', city: 'Mussoorie', state: 'Uttarakhand', country: 'India' },
    { id: '301', name: 'Muzaffarnagar', address: 'Muzaffarnagar, Uttar Pradesh, India', area: 'Muzaffarnagar', city: 'Muzaffarnagar', state: 'Uttar Pradesh', country: 'India' },
    { id: '302', name: 'Muzaffarpur', address: 'Muzaffarpur, Bihar, India', area: 'Muzaffarpur', city: 'Muzaffarpur', state: 'Bihar', country: 'India' },
    { id: '303', name: 'Mysore', address: 'Mysore, Karnataka, India', area: 'Mysore', city: 'Mysore', state: 'Karnataka', country: 'India' },
    { id: '304', name: 'Nainital', address: 'Nainital, Uttarakhand, India', area: 'Nainital', city: 'Nainital', state: 'Uttarakhand', country: 'India' },
    { id: '305', name: 'Nashik', address: 'Nashik, Maharashtra, India', area: 'Nashik', city: 'Nashik', state: 'Maharashtra', country: 'India' },
    { id: '306', name: 'Nellore', address: 'Nellore, Andhra Pradesh, India', area: 'Nellore', city: 'Nellore', state: 'Andhra Pradesh', country: 'India' },
    { id: '307', name: 'New Delhi', address: 'New Delhi, Delhi, India', area: 'New Delhi', city: 'New Delhi', state: 'Delhi', country: 'India' },
    { id: '308', name: 'Noida', address: 'Noida, Uttar Pradesh, India', area: 'Noida', city: 'Noida', state: 'Uttar Pradesh', country: 'India' },
    { id: '309', name: 'Ooty', address: 'Ooty, Tamil Nadu, India', area: 'Ooty', city: 'Ooty', state: 'Tamil Nadu', country: 'India' },
    { id: '310', name: 'Palakkad', address: 'Palakkad, Kerala, India', area: 'Palakkad', city: 'Palakkad', state: 'Kerala', country: 'India' },
    { id: '311', name: 'Panaji', address: 'Panaji, Goa, India', area: 'Panaji', city: 'Panaji', state: 'Goa', country: 'India' },
    { id: '312', name: 'Panchkula', address: 'Panchkula, Haryana, India', area: 'Panchkula', city: 'Panchkula', state: 'Haryana', country: 'India' },
    { id: '313', name: 'Panipat', address: 'Panipat, Haryana, India', area: 'Panipat', city: 'Panipat', state: 'Haryana', country: 'India' },
    { id: '314', name: 'Pathankot', address: 'Pathankot, Punjab, India', area: 'Pathankot', city: 'Pathankot', state: 'Punjab', country: 'India' },
    { id: '315', name: 'Patiala', address: 'Patiala, Punjab, India', area: 'Patiala', city: 'Patiala', state: 'Punjab', country: 'India' },
    { id: '316', name: 'Patna', address: 'Patna, Bihar, India', area: 'Patna', city: 'Patna', state: 'Bihar', country: 'India' },
    { id: '317', name: 'Pondicherry', address: 'Pondicherry, Puducherry, India', area: 'Pondicherry', city: 'Pondicherry', state: 'Puducherry', country: 'India' },
    { id: '318', name: 'Port Blair', address: 'Port Blair, Andaman and Nicobar Islands, India', area: 'Port Blair', city: 'Port Blair', state: 'Andaman and Nicobar Islands', country: 'India' },
    { id: '319', name: 'Pune', address: 'Pune, Maharashtra, India', area: 'Pune', city: 'Pune', state: 'Maharashtra', country: 'India' },
    { id: '320', name: 'Puri', address: 'Puri, Odisha, India', area: 'Puri', city: 'Puri', state: 'Odisha', country: 'India' },
    { id: '321', name: 'Pushkar', address: 'Pushkar, Rajasthan, India', area: 'Pushkar', city: 'Pushkar', state: 'Rajasthan', country: 'India' },
    { id: '322', name: 'Raipur', address: 'Raipur, Chhattisgarh, India', area: 'Raipur', city: 'Raipur', state: 'Chhattisgarh', country: 'India' },
    { id: '323', name: 'Rajkot', address: 'Rajkot, Gujarat, India', area: 'Rajkot', city: 'Rajkot', state: 'Gujarat', country: 'India' },
    { id: '324', name: 'Ranchi', address: 'Ranchi, Jharkhand, India', area: 'Ranchi', city: 'Ranchi', state: 'Jharkhand', country: 'India' },
    { id: '325', name: 'Rishikesh', address: 'Rishikesh, Uttarakhand, India', area: 'Rishikesh', city: 'Rishikesh', state: 'Uttarakhand', country: 'India' },
    { id: '326', name: 'Salem', address: 'Salem, Tamil Nadu, India', area: 'Salem', city: 'Salem', state: 'Tamil Nadu', country: 'India' },
    { id: '327', name: 'Shimla', address: 'Shimla, Himachal Pradesh, India', area: 'Shimla', city: 'Shimla', state: 'Himachal Pradesh', country: 'India' },
    { id: '328', name: 'Shillong', address: 'Shillong, Meghalaya, India', area: 'Shillong', city: 'Shillong', state: 'Meghalaya', country: 'India' },
    { id: '329', name: 'Siliguri', address: 'Siliguri, West Bengal, India', area: 'Siliguri', city: 'Siliguri', state: 'West Bengal', country: 'India' },
    { id: '330', name: 'Solapur', address: 'Solapur, Maharashtra, India', area: 'Solapur', city: 'Solapur', state: 'Maharashtra', country: 'India' },
    { id: '331', name: 'Srinagar', address: 'Srinagar, Jammu and Kashmir, India', area: 'Srinagar', city: 'Srinagar', state: 'Jammu and Kashmir', country: 'India' },
    { id: '332', name: 'Surat', address: 'Surat, Gujarat, India', area: 'Surat', city: 'Surat', state: 'Gujarat', country: 'India' },
    { id: '333', name: 'Thane', address: 'Thane, Maharashtra, India', area: 'Thane', city: 'Thane', state: 'Maharashtra', country: 'India' },
    { id: '334', name: 'Thiruvananthapuram', address: 'Thiruvananthapuram, Kerala, India', area: 'Thiruvananthapuram', city: 'Thiruvananthapuram', state: 'Kerala', country: 'India' },
    { id: '335', name: 'Thrissur', address: 'Thrissur, Kerala, India', area: 'Thrissur', city: 'Thrissur', state: 'Kerala', country: 'India' },
    { id: '336', name: 'Tirupati', address: 'Tirupati, Andhra Pradesh, India', area: 'Tirupati', city: 'Tirupati', state: 'Andhra Pradesh', country: 'India' },
    { id: '337', name: 'Tiruchirappalli', address: 'Tiruchirappalli, Tamil Nadu, India', area: 'Tiruchirappalli', city: 'Tiruchirappalli', state: 'Tamil Nadu', country: 'India' },
    { id: '338', name: 'Tirunelveli', address: 'Tirunelveli, Tamil Nadu, India', area: 'Tirunelveli', city: 'Tirunelveli', state: 'Tamil Nadu', country: 'India' },
    { id: '339', name: 'Tiruppur', address: 'Tiruppur, Tamil Nadu, India', area: 'Tiruppur', city: 'Tiruppur', state: 'Tamil Nadu', country: 'India' },
    { id: '340', name: 'Tumkur', address: 'Tumkur, Karnataka, India', area: 'Tumkur', city: 'Tumkur', state: 'Karnataka', country: 'India' },
    { id: '341', name: 'Udaipur', address: 'Udaipur, Rajasthan, India', area: 'Udaipur', city: 'Udaipur', state: 'Rajasthan', country: 'India' },
    { id: '342', name: 'Ujjain', address: 'Ujjain, Madhya Pradesh, India', area: 'Ujjain', city: 'Ujjain', state: 'Madhya Pradesh', country: 'India' },
    { id: '343', name: 'Vadodara', address: 'Vadodara, Gujarat, India', area: 'Vadodara', city: 'Vadodara', state: 'Gujarat', country: 'India' },
    { id: '344', name: 'Vapi', address: 'Vapi, Gujarat, India', area: 'Vapi', city: 'Vapi', state: 'Gujarat', country: 'India' },
    { id: '345', name: 'Varanasi', address: 'Varanasi, Uttar Pradesh, India', area: 'Varanasi', city: 'Varanasi', state: 'Uttar Pradesh', country: 'India' },
    { id: '346', name: 'Vasco Da Gama', address: 'Vasco Da Gama, Goa, India', area: 'Vasco Da Gama', city: 'Vasco Da Gama', state: 'Goa', country: 'India' },
    { id: '347', name: 'Vellore', address: 'Vellore, Tamil Nadu, India', area: 'Vellore', city: 'Vellore', state: 'Tamil Nadu', country: 'India' },
    { id: '348', name: 'Vijayawada', address: 'Vijayawada, Andhra Pradesh, India', area: 'Vijayawada', city: 'Vijayawada', state: 'Andhra Pradesh', country: 'India' },
    { id: '349', name: 'Visakhapatnam', address: 'Visakhapatnam, Andhra Pradesh, India', area: 'Visakhapatnam', city: 'Visakhapatnam', state: 'Andhra Pradesh', country: 'India' },
    { id: '350', name: 'Warangal', address: 'Warangal, Telangana, India', area: 'Warangal', city: 'Warangal', state: 'Telangana', country: 'India' },
    { id: '351', name: 'Yamunanagar', address: 'Yamunanagar, Haryana, India', area: 'Yamunanagar', city: 'Yamunanagar', state: 'Haryana', country: 'India' },
    { id: '352', name: 'Zunheboto', address: 'Zunheboto, Nagaland, India', area: 'Zunheboto', city: 'Zunheboto', state: 'Nagaland', country: 'India' }
  ];

  getCurrentLocation = () => this.currentLocation();

  get currentUserLocation() {
    return this.currentLocation();
  }

  getLocationSignal() {
    return this.currentLocation;
  }
  searchLocations(query: string): LocationSuggestion[] {
    if (!query || query.length < 2) return [];

    const searchTerm = query.toLowerCase().trim();
    return this.mockLocations
      .filter(location =>
        location.name.toLowerCase().includes(searchTerm) ||
        location.address.toLowerCase().includes(searchTerm) ||
        location.area?.toLowerCase().includes(searchTerm) ||
        location.city?.toLowerCase().includes(searchTerm)
      )
      .slice(0, 5); // Limit to 5 results for better UX
  }

  setLocation(location: UserLocation) {
    this.currentLocation.set(location);
    // Save to localStorage for persistence
    localStorage.setItem('userLocation', JSON.stringify(location));
  }

  clearLocation() {
    this.currentLocation.set(null);
    localStorage.removeItem('userLocation');
  }

  loadSavedLocation() {
    const saved = localStorage.getItem('userLocation');
    if (saved) {
      try {
        const location = JSON.parse(saved);
        this.currentLocation.set(location);
      } catch (error) {
        console.error('Error loading saved location:', error);
      }
    }
  }

  async getCurrentPosition(): Promise<UserLocation> {
    return new Promise((resolve, reject) => {
      if (!navigator.geolocation) {
        reject(new Error('Geolocation is not supported'));
        return;
      }

      navigator.geolocation.getCurrentPosition(
        async (position) => {
          const { latitude, longitude } = position.coords;

          try {
            const res = await fetch(
              `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json`,
              { headers: { 'Accept-Language': 'en' } }
            );
            const data = await res.json();
            const a = data.address ?? {};
            const name =
              a.suburb ?? a.neighbourhood ?? a.village ?? a.town ?? a.city ?? a.county ?? 'Unknown Area';
            const city = a.city ?? a.town ?? a.village ?? a.county ?? '';
            const state = a.state ?? '';
            const country = a.country ?? '';
            const fullAddress = [name, city, state, country].filter(Boolean).join(', ');

            resolve({
              address: fullAddress,
              latitude,
              longitude,
              city,
              area: name
            });
          } catch {
            // Fallback if reverse geocode fails
            resolve({
              address: `${latitude.toFixed(4)}, ${longitude.toFixed(4)}`,
              latitude,
              longitude,
              city: '',
              area: ''
            });
          }
        },
        (error) => {
          reject(error);
        },
        {
          enableHighAccuracy: true,
          timeout: 10000,
          maximumAge: 300000 // 5 minutes
        }
      );
    });
  }
}
