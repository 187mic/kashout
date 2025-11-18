import { supabase } from './src/lib/supabase';

interface TestResult {
  step: string;
  status: 'success' | 'error';
  message: string;
  data?: any;
}

const results: TestResult[] = [];

function logResult(step: string, status: 'success' | 'error', message: string, data?: any) {
  results.push({ step, status, message, data });
  console.log(`\n[${status.toUpperCase()}] ${step}`);
  console.log(`  ${message}`);
  if (data) {
    console.log('  Data:', JSON.stringify(data, null, 2));
  }
}

async function testVerificationSystem() {
  console.log('🚀 Testing Verification System with Fake Data\n');
  console.log('='.repeat(60));

  try {
    // Test 1: Create a test user
    console.log('\n📝 Step 1: Creating test user...');
    const testEmail = `test.user.${Date.now()}@kashout-test.com`;
    const testPassword = 'TestPassword123!';
    
    const { data: authData, error: authError } = await supabase.auth.signUp({
      email: testEmail,
      password: testPassword,
    });

    if (authError) throw authError;
    if (!authData.user) throw new Error('User creation failed');

    logResult('Create User', 'success', `Test user created: ${testEmail}`, { userId: authData.user.id });

    const userId = authData.user.id;

    // Test 2: Submit verification documents
    console.log('\n📤 Step 2: Submitting verification documents...');
    
    const verificationData = {
      user_id: userId,
      document_type: 'passport',
      document_number: 'AB1234567',
      first_name: 'John',
      last_name: 'Doe',
      date_of_birth: '1990-05-15',
      address_line1: '123 Main Street',
      city: 'New York',
      state: 'NY',
      postal_code: '10001',
      country: 'US',
      document_front_url: 'https://example.com/fake-passport-front.jpg',
      document_back_url: 'https://example.com/fake-passport-back.jpg',
      selfie_url: 'https://example.com/fake-selfie.jpg',
      status: 'pending',
      submitted_at: new Date().toISOString(),
    };

    const { data: verification, error: verifyError } = await supabase
      .from('user_verifications')
      .insert(verificationData)
      .select()
      .single();

    if (verifyError) throw verifyError;

    logResult('Submit Verification', 'success', 'Verification documents submitted', verification);

    // Test 3: Check verification status
    console.log('\n🔍 Step 3: Checking verification status...');
    
    const { data: statusData, error: statusError } = await supabase
      .from('user_verifications')
      .select('*')
      .eq('user_id', userId)
      .single();

    if (statusError) throw statusError;

    logResult('Check Status', 'success', `Verification status: ${statusData.status}`, statusData);

    // Test 4: Simulate admin approval
    console.log('\n✅ Step 4: Simulating admin approval...');
    
    const { data: approveData, error: approveError } = await supabase
      .from('user_verifications')
      .update({
        status: 'approved',
        verified_at: new Date().toISOString(),
        reviewed_by: 'system-test',
        notes: 'Auto-approved for testing purposes',
      })
      .eq('user_id', userId)
      .select()
      .single();

    if (approveError) throw approveError;

    logResult('Approve Verification', 'success', 'Verification approved', approveData);

    // Test 5: Update user profile verification status
    console.log('\n👤 Step 5: Updating user profile...');
    
    const { data: profileData, error: profileError } = await supabase
      .from('profiles')
      .update({
        verification_status: 'verified',
        kyc_completed: true,
      })
      .eq('id', userId)
      .select()
      .single();

    if (profileError) throw profileError;

    logResult('Update Profile', 'success', 'User profile updated', profileData);

    // Test 6: Test rejection scenario with a new user
    console.log('\n❌ Step 6: Testing rejection scenario...');
    
    const testEmail2 = `test.reject.${Date.now()}@kashout-test.com`;
    const { data: authData2, error: authError2 } = await supabase.auth.signUp({
      email: testEmail2,
      password: testPassword,
    });

    if (authError2) throw authError2;
    if (!authData2.user) throw new Error('Second user creation failed');

    const userId2 = authData2.user.id;

    const { data: verification2, error: verifyError2 } = await supabase
      .from('user_verifications')
      .insert({
        ...verificationData,
        user_id: userId2,
        document_number: 'CD9876543',
      })
      .select()
      .single();

    if (verifyError2) throw verifyError2;

    const { data: rejectData, error: rejectError } = await supabase
      .from('user_verifications')
      .update({
        status: 'rejected',
        reviewed_by: 'system-test',
        notes: 'Document quality too poor - test rejection',
      })
      .eq('user_id', userId2)
      .select()
      .single();

    if (rejectError) throw rejectError;

    logResult('Reject Verification', 'success', 'Verification rejected', rejectData);

    // Final Summary
    console.log('\n' + '='.repeat(60));
    console.log('\n📊 TEST SUMMARY\n');
    
    const successCount = results.filter(r => r.status === 'success').length;
    const errorCount = results.filter(r => r.status === 'error').length;
    
    console.log(`✅ Successful: ${successCount}`);
    console.log(`❌ Failed: ${errorCount}`);
    console.log(`📝 Total Tests: ${results.length}`);
    
    console.log('\n✨ All verification system tests completed successfully!\n');
    console.log('Test users created:');
    console.log(`  - ${testEmail} (approved)`);
    console.log(`  - ${testEmail2} (rejected)`);

  } catch (error) {
    logResult('Test Execution', 'error', `Test failed: ${error.message}`, error);
    console.error('\n❌ Test execution failed:', error);
    process.exit(1);
  }
}

// Run the tests
testVerificationSystem()
  .then(() => {
    console.log('\n✅ Test script completed');
    process.exit(0);
  })
  .catch((error) => {
    console.error('\n💥 Fatal error:', error);
    process.exit(1);
  });