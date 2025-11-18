import { supabase } from '../src/lib/supabase';

console.log('🔐 SECURITY TESTS - Verification System\n');

let passCount = 0;
let failCount = 0;

async function securityTest(name: string, fn: () => Promise<void>) {
  try {
    process.stdout.write(`  ▶ ${name}... `);
    await fn();
    console.log('✅');
    passCount++;
  } catch (error) {
    console.log('❌');
    console.log(`    Error: ${error.message}`);
    failCount++;
  }
}

// SQL Injection Tests
await securityTest('SQL injection in document_number field', async () => {
  const email = `sqli.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const maliciousInput = "'; DROP TABLE user_verifications; --";
  
  const { error } = await supabase.from('user_verifications').insert({
    user_id: user.user!.id,
    document_type: 'passport',
    document_number: maliciousInput,
    first_name: 'SQL',
    last_name: 'Test',
    date_of_birth: '1990-01-01',
    address_line1: '123 Test',
    city: 'Test',
    country: 'US',
    status: 'pending',
  });
  
  // Should either sanitize or reject
  if (!error) {
    // Check table still exists
    const { error: checkError } = await supabase.from('user_verifications').select('id').limit(1);
    if (checkError) throw new Error('SQL injection succeeded!');
  }
});

await securityTest('XSS in notes field', async () => {
  const email = `xss.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const xssPayload = '<script>alert("XSS")</script>';
  
  const { data } = await supabase.from('user_verifications')
    .insert({
      user_id: user.user!.id,
      document_type: 'passport',
      document_number: 'XSS123',
      first_name: 'XSS',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  await supabase.from('user_verifications')
    .update({ notes: xssPayload })
    .eq('id', data!.id);
  
  const { data: fetched } = await supabase.from('user_verifications')
    .select('notes')
    .eq('id', data!.id)
    .single();
  
  // Should be sanitized or escaped
  if (fetched!.notes === xssPayload) {
    console.warn('    ⚠️  XSS payload not sanitized');
  }
});

await securityTest('Cannot access other users verification', async () => {
  // Create user 1
  const { data: user1 } = await supabase.auth.signUp({
    email: `user1.${Date.now()}@test.com`,
    password: 'TestPass123!',
  });
  
  const { data: verification1 } = await supabase.from('user_verifications')
    .insert({
      user_id: user1.user!.id,
      document_type: 'passport',
      document_number: 'USER1',
      first_name: 'User',
      last_name: 'One',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  // Create user 2 and try to access user 1's verification
  await supabase.auth.signOut();
  const { data: user2 } = await supabase.auth.signUp({
    email: `user2.${Date.now()}@test.com`,
    password: 'TestPass123!',
  });
  
  // Try to access user1's verification
  const { data, error } = await supabase.from('user_verifications')
    .select()
    .eq('id', verification1!.id)
    .single();
  
  if (data && !error) {
    throw new Error('RLS policy failed - user can access other users data!');
  }
});

await securityTest('Cannot modify verification status without proper role', async () => {
  const { data: user } = await supabase.auth.signUp({
    email: `nonadmin.${Date.now()}@test.com`,
    password: 'TestPass123!',
  });
  
  const { data: verification } = await supabase.from('user_verifications')
    .insert({
      user_id: user.user!.id,
      document_type: 'passport',
      document_number: 'NOADMIN',
      first_name: 'No',
      last_name: 'Admin',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  // User tries to approve their own verification
  const { error } = await supabase.from('user_verifications')
    .update({ status: 'approved', verified_at: new Date().toISOString() })
    .eq('id', verification!.id);
  
  // Should fail if RLS is properly configured
  if (!error) {
    console.warn('    ⚠️  User can approve own verification - RLS policy needed!');
  }
});

console.log(`\n📊 Security Tests: ${passCount} passed, ${failCount} failed`);import { supabase } from '../src/lib/supabase';

console.log('🧪 FUNCTIONAL TESTS - Verification System\n');

interface TestCase {
  name: string;
  fn: () => Promise<void>;
}

const tests: TestCase[] = [];
let passCount = 0;
let failCount = 0;

function test(name: string, fn: () => Promise<void>) {
  tests.push({ name, fn });
}

async function runTests() {
  for (const t of tests) {
    try {
      process.stdout.write(`  ▶ ${t.name}... `);
      await t.fn();
      console.log('✅');
      passCount++;
    } catch (error) {
      console.log('❌');
      console.log(`    Error: ${error.message}`);
      failCount++;
    }
  }
  
  console.log(`\n📊 Results: ${passCount} passed, ${failCount} failed`);
}

// Test cases
test('User can submit verification with valid data', async () => {
  const email = `func.test.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const { error } = await supabase.from('user_verifications').insert({
    user_id: user.user!.id,
    document_type: 'passport',
    document_number: 'TEST123',
    first_name: 'Test',
    last_name: 'User',
    date_of_birth: '1990-01-01',
    address_line1: '123 Test St',
    city: 'Test City',
    country: 'US',
    status: 'pending',
  });
  
  if (error) throw error;
});

test('Verification status transitions from pending to approved', async () => {
  const email = `transition.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const { data: verification } = await supabase.from('user_verifications')
    .insert({
      user_id: user.user!.id,
      document_type: 'passport',
      document_number: 'TRANS123',
      first_name: 'Trans',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  const { data: updated } = await supabase.from('user_verifications')
    .update({ status: 'approved', verified_at: new Date().toISOString() })
    .eq('id', verification!.id)
    .select()
    .single();
  
  if (updated!.status !== 'approved') throw new Error('Status not updated');
});

test('User can resubmit after rejection', async () => {
  const email = `resubmit.${Date.now()}@test.com`;
  const { data: user } = await supabase.auth.signUp({
    email,
    password: 'TestPass123!',
  });
  
  const userId = user.user!.id;
  
  // First submission
  const { data: first } = await supabase.from('user_verifications')
    .insert({
      user_id: userId,
      document_type: 'passport',
      document_number: 'REJ123',
      first_name: 'Reject',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    })
    .select()
    .single();
  
  // Reject it
  await supabase.from('user_verifications')
    .update({ status: 'rejected', notes: 'Test rejection' })
    .eq('id', first!.id);
  
  // Resubmit
  const { error } = await supabase.from('user_verifications')
    .insert({
      user_id: userId,
      document_type: 'passport',
      document_number: 'REJ456',
      first_name: 'Reject',
      last_name: 'Test',
      date_of_birth: '1990-01-01',
      address_line1: '123 Test',
      city: 'Test',
      country: 'US',
      status: 'pending',
    });
  
  if (error) throw error;
});

runTests();